using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace XiEditor
{
	class CoreConnection
	{
		Stream inHandle;
		byte[] recvBuf;
		byte[] sizeBuf;
		int rpcIndex;

		Dictionary<int, Action<object>> pending;

		Action<object> callback;
		
		public event EventHandler ProcessExited;

		public CoreConnection(string filename, Action<object> cb)
		{
			var myProcess = new Process();

			sizeBuf = new byte[8];
			recvBuf = new byte[65536];

			pending = new Dictionary<int, Action<object>>();

			rpcIndex = 0;

			myProcess.StartInfo.FileName = filename;
			myProcess.StartInfo.CreateNoWindow = true;

			myProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
			myProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;

			myProcess.StartInfo.RedirectStandardInput = true;
			myProcess.StartInfo.RedirectStandardOutput = true;
			myProcess.StartInfo.RedirectStandardError = true;

			myProcess.StartInfo.UseShellExecute = false;

			callback = cb;

			// Gives us callback on exit
			myProcess.EnableRaisingEvents = true;

			myProcess.ErrorDataReceived += errHander;
			myProcess.OutputDataReceived += recvHandler;
			myProcess.Exited += ProcessExited;
			myProcess.Start();
			myProcess.BeginErrorReadLine();
			myProcess.BeginOutputReadLine();
			inHandle = myProcess.StandardInput.BaseStream;
		}

		private void recvHandler(object sender, DataReceivedEventArgs e)
		{
			handleRaw(e.Data);
		}

		private void handleRaw(string data)
		{
			try
			{
				var resp = JsonConvert.DeserializeObject(data);
				if (!handleRpcResponse(resp))
				{
					callback(resp);
				}
			} catch (JsonSerializationException)
			{
				Console.WriteLine("json error");
			}
		}

		private bool handleRpcResponse(object data)
		{
			dynamic resp = data;
			if (resp["id"] != null)
			{
				var index = (int)resp["id"];
				dynamic result = resp["result"];
				var callback = null as Action<object>;
				callback = pending[index];
				pending.Remove(index);
				callback(result);
				return true;
			} else
			{
				return false;
			}
		}

		public void send(string json)
		{
			var data = Encoding.UTF8.GetBytes(json);
			inHandle.Write(data, 0, data.Length);
			inHandle.Flush();
		}

		public void sendJson(object json)
		{
			string data = JsonConvert.SerializeObject(json) + '\n';
			send(data);
		}

		private void Exited(object sender, EventArgs e)
		{
			throw new Exception("xicore.exe crashed");
		}

		private void errHander(object sender, DataReceivedEventArgs e)
		{
			// Console.WriteLine(e.Data.ToString());
		}

		public void sendRpcAsync(string method, object parameters, Action<object> callback = null)
		{
			
			var req = new Dictionary<string, dynamic> { { "method", method }, { "params", parameters } };
			if (callback != null)
			{
				req.Add("id",  rpcIndex);
				var index = rpcIndex;
				rpcIndex++;
				pending.Add(index, callback);
			}
			sendJson(req);
		}

		public object sendRpc(string m, object p)
		{
			var result = null as object;
			var e = new AutoResetEvent(false);
			sendRpcAsync(m, p, delegate(object data) {
				result = data;
				e.Set();
			});
			e.WaitOne();
			return result;
		}
	}
}
