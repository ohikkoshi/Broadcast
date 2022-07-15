using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace Flex.Net.Sockets
{
	public class NIC
	{
		static List<NetworkInterface> adapters = new();
		static List<NetworkInterface> Adapters
		{
			get {
#if (UNITY_EDITOR || UNITY_STANDALONE)
				if (adapters.Count == 0) {
					var query = NetworkInterface.GetAllNetworkInterfaces();

					foreach (var nic in query) {
						if (nic.OperationalStatus != OperationalStatus.Up) {
							continue;
						}

						if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) {
							continue;
						}

						//var type = nic.NetworkInterfaceType.ToString();
						var name = nic.Name.ToLower();
						var desc = nic.Description.ToLower();
						var key = $"{name}|{desc}";

						if (
#if (UNITY_EDITOR || UNITY_STANDALONE)
							key.IndexOf("wi-fi direct virtual adapter") < 0 &&
							(
							// Windows
							key.IndexOf("wifi") >= 0 ||
							key.IndexOf("wi-fi") >= 0 ||
							key.IndexOf("wireless") >= 0 ||
							// macOS
							key.IndexOf("en1") >= 0
							)
#elif UNITY_IOS
							// iOS
							key.IndexOf("en0") >= 0 ||
#elif UNITY_ANDROID
							// Android
							key.IndexOf("wlan") >= 0 ||
#endif
						) {
							adapters.Add(nic);
						}
					}
				}
#endif
				return adapters;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string IPv4()
		{
			// Wi-Fi
			foreach (var nic in Adapters) {
				var props = nic.GetIPProperties().UnicastAddresses;

				foreach (var prop in props) {
					if (prop.Address.AddressFamily == AddressFamily.InterNetwork) {
						return prop.Address.ToString();
					}
				}
			}

			// LAN
			return Ethernet();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static string Ethernet()
		{
			foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()) {
				if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet && nic.OperationalStatus == OperationalStatus.Up) {
					foreach (var unicast in nic.GetIPProperties().UnicastAddresses) {
						if (unicast.Address.AddressFamily == AddressFamily.InterNetwork) {
							return unicast.Address.ToString();
						}
					}
				}
			}

			return null;
		}
	}
}
