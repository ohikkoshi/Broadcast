using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace Flex.Net.Sockets
{
	public class NIC
	{
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

		public static string IPv4(NetworkInterfaceType networkInterfaceType)
		{
			try {
				var array = NetworkInterface.GetAllNetworkInterfaces();
				const string LocalLoopbackAddress = "127.0";
				const string LinkLocalAddress = "169.254.";

				foreach (var nic in array) {
					if (nic.NetworkInterfaceType == networkInterfaceType) {
						var props = nic.GetIPProperties();

						foreach (var ip in props.UnicastAddresses) {
							if (ip.Address.AddressFamily == AddressFamily.InterNetwork) {
								var addr = ip.Address.ToString();

								if (string.IsNullOrEmpty(addr) ||
									addr.StartsWith(LocalLoopbackAddress) ||
									addr.StartsWith(LinkLocalAddress)) {
									return null;
								}

								return addr;
							}
						}
					}
				}
			} catch (System.Exception) {
				throw;
			}

			return null;
		}
	}
}
