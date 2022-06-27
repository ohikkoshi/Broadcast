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
#if (UNITY_EDITOR || UNITY_STANDALONE)
		static List<NetworkInterface> adapters = new();
		static List<NetworkInterface> Adapters
		{
			get {
				if (adapters.Count == 0) {
					var query = NetworkInterface.GetAllNetworkInterfaces();

					foreach (var nic in query) {
						var name = nic.Name.ToLower();
						var desc = nic.Description.ToLower();
						var type = nic.NetworkInterfaceType.ToString();
						//UnityEngine.Debug.Log($"{type}:{name}:{desc}");

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
						if (name.IndexOf("wi-fi direct virtual adapter") >= 0 ||
							desc.IndexOf("wi-fi direct virtual adapter") >= 0) {
							// Ignore Adapter
						} else
#endif
						if (
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
							// Windows
							name.IndexOf("wifi") >= 0 ||
							name.IndexOf("wi-fi") >= 0 ||
							name.IndexOf("wireless") >= 0 ||
							desc.IndexOf("wifi") >= 0 ||
							desc.IndexOf("wi-fi") >= 0 ||
							desc.IndexOf("wireless") >= 0
#elif (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
							// macOS
							name.IndexOf("en1") >= 0 ||
							desc.IndexOf("en1") >= 0
#elif UNITY_IOS
							// iOS
							name.IndexOf("en0") >= 0 ||
							desc.IndexOf("en0") >= 0
#elif UNITY_ANDROID
							// Android
							name.IndexOf("wlan") >= 0 ||
							desc.IndexOf("wlan") >= 0
#endif
						) {
							adapters.Add(nic);
						}
					}
				}

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
			foreach (var nic in Adapters) {
				var props = nic.GetIPProperties().UnicastAddresses;

				foreach (var prop in props) {
					if (prop.Address.AddressFamily == AddressFamily.InterNetwork) {
						return prop.Address.ToString();
					}
				}
			}

			return null;
		}
#else
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string IPv4()
		{
			try {
				var hostName = Dns.GetHostName();
				var addresses = Dns.GetHostAddresses(hostName);

				foreach (var address in addresses) {
					if (address.AddressFamily == AddressFamily.InterNetwork) {
						return address.ToString();
					}
				}
			} catch (System.Exception) {
				throw;
			}

			return null;
		}
#endif
	}
}
