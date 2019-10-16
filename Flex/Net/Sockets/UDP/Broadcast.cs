using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Flex.Net.Sockets
{
	public class Broadcast
	{
		// Properties
		public bool Reachability { get; }
		public string HostName { get; }
		public string ClientName { get; }
		public bool Connected => (sender != null || receiver != null);

		// Initializer
		public int SendBufferSize { get; } = 1024;
		public int SendTimeout { get; } = 0;
		public int ReceiveBufferSize { get; } = 4096;
		public int ReceiveTimeout { get; } = 0;

		// Handler
		public Action<string> OnError;

		// UDPClient
		UdpClient sender;
		UdpClient receiver;
		CancellationTokenSource token;


		public Broadcast()
		{
			try {
				var address = NIC.IPv4(NetworkInterfaceType.Wireless80211);

				if (string.IsNullOrEmpty(address)) {
					address = NIC.IPv4(NetworkInterfaceType.Ethernet);
				}

				if (string.IsNullOrEmpty(address)) {
					address = NIC.IPv4();
				}

				if (!string.IsNullOrEmpty(address)) {
					var array = address.Split('.');
					HostName = $"{array[0]}.{array[1]}.{array[2]}.255";
					ClientName = address;
					Reachability = true;
				}
			} catch (Exception e) {
				Close(e.Message);
			}
		}

		public Broadcast(int sendBufferSize, int sendTimeout, int receiveBufferSize, int receiveTimeout) : this()
		{
			Debug.Assert(sendBufferSize > 0);
			SendBufferSize = sendBufferSize;

			Debug.Assert(sendTimeout >= 0);
			SendTimeout = sendTimeout;

			Debug.Assert(receiveBufferSize > 0);
			ReceiveBufferSize = receiveBufferSize;

			Debug.Assert(receiveTimeout >= 0);
			ReceiveTimeout = receiveTimeout;
		}

		~Broadcast()
		{
			Close();
		}

		public void Close(string msg = null)
		{
			if (!string.IsNullOrEmpty(msg)) {
				Log.d(msg);
				OnError?.Invoke(msg);
			}

			if (token != null) {
				token.Cancel();
				token = null;
			}

			if (sender != null) {
				sender.Close();
				sender = null;
			}

			if (receiver != null) {
				receiver.Close();
				receiver = null;
			}
		}

		public void Connect(int port)
		{
			Connect(HostName, port);
		}

		public void Connect(string hostname, int port)
		{
			Debug.Assert(0 <= port && port <= 65535);

			sender?.Close();
			sender = new UdpClient();
			sender.Client.SendBufferSize = SendBufferSize;
			sender.Client.SendTimeout = SendTimeout;
			sender.Client.EnableBroadcast = (hostname == HostName);
			sender.Client.ExclusiveAddressUse = false;
			sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

			try {
				if (!string.IsNullOrEmpty(hostname)) {
					sender.Connect(hostname, port);
				}
			} catch (Exception e) {
				Close(e.Message);
			}
		}

		public void Accept(int port, Action<byte[]> callback = null)
		{
			Accept(IPAddress.Any, port, callback);
		}

		public void Accept(string host, int port, Action<byte[]> callback = null)
		{
			Accept(IPAddress.Parse(host), port, callback);
		}

		public void Accept(IPAddress address, int port, Action<byte[]> callback = null)
		{
			Debug.Assert(0 <= port && port <= 65535);

			receiver?.Close();
			receiver = new UdpClient(port);
			receiver.Client.ReceiveBufferSize = ReceiveBufferSize;
			receiver.Client.ReceiveTimeout = ReceiveTimeout;
			receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

			token?.Cancel();
			token = new CancellationTokenSource();

			var endPoint = new IPEndPoint(address, port);
			var context = SynchronizationContext.Current;

			Task.Run(() => {
				while (!token.IsCancellationRequested) {
					try {
						byte[] data = receiver.Receive(ref endPoint);

						if (data?.Length > 0) {
							context.Post(_ => {
								callback?.Invoke(data);
							}, null);
						}
					} catch (Exception e) {
						Close(e.Message);
					} finally {
						Thread.Yield();
					}
				}
			}, token.Token);
		}

		public void Send(byte[] data, AsyncCallback callback = null)
		{
			if (data == null || data.Length == 0) {
				return;
			}

			try {
				sender?.BeginSend(data, data.Length, callback, sender);
			} catch (Exception e) {
				Close(e.Message);
			}
		}

		public void Send(byte[] data, string hostName, int port, AsyncCallback callback = null)
		{
			Debug.Assert(!string.IsNullOrEmpty(hostName));
			Debug.Assert(0 <= port && port <= 65535);

			if (data == null || data.Length == 0) {
				return;
			}

			try {
				sender?.BeginSend(data, data.Length, hostName, port, callback, sender);
			} catch (Exception e) {
				Close(e.Message);
			}
		}

		public void Send(byte[] data, IPEndPoint endPoint, AsyncCallback callback = null)
		{
			Debug.Assert(endPoint != null);

			if (data == null || data.Length == 0) {
				return;
			}

			try {
				sender?.BeginSend(data, data.Length, endPoint, callback, sender);
			} catch (Exception e) {
				Close(e.Message);
			}
		}
	}
}
