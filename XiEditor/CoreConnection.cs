using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace XiEditor
{
	class CoreConnection
	{
		BinaryReader outHandle;
		BinaryWriter inHandle;
		byte[] recvBuf;
		byte[] sizeBuf;
		int rpcIndex;

		public event EventHandler<string> DataReceived;
		public event EventHandler<string> ErrorReceived;
		public event EventHandler<EventArgs> ProcessExited;

		public CoreConnection()
		{
			var myProcess = new Process();

			sizeBuf = new byte[8];
			recvBuf = new byte[65536];

			rpcIndex = 0;

			myProcess.StartInfo.FileName = "xicore.exe";
			myProcess.StartInfo.CreateNoWindow = true;
			myProcess.StartInfo.RedirectStandardError = true;
			myProcess.StartInfo.RedirectStandardOutput = true;
			myProcess.StartInfo.RedirectStandardInput = true;
			myProcess.StartInfo.UseShellExecute = false;

			// Gives us callback on exit
			myProcess.EnableRaisingEvents = true;

			myProcess.ErrorDataReceived += ErrorDataReceived;
			myProcess.Exited += Exited;
			myProcess.Start();
			myProcess.BeginErrorReadLine();
			inHandle = new BinaryWriter(myProcess.StandardInput.BaseStream);
			outHandle = new BinaryReader(myProcess.StandardOutput.BaseStream);

			Task.Factory.StartNew(() => ReceiveData());
		}
		
		public void Send(string json)
		{
			var data = Encoding.UTF8.GetBytes(json);
			var length = (ulong)data.Length;
			for (var i = 0; i < 8; i++)
			{
				sizeBuf[i] = (byte)(length >> (i * 8) & 0xff);
			}
			inHandle.Write(sizeBuf, 0, sizeBuf.Length);
			inHandle.Write(data, 0, data.Length);
			inHandle.Flush();
		}

		public void SendRpc(object data)
		{
			var index = rpcIndex;
			rpcIndex++;
			var json = new object[] { "rpc", new object[] { new { index = index, request = data } } };
			SendJson(json);
		}

		public void SendJson(object json)
		{
			string data = JsonConvert.SerializeObject(json);
			Send(data);
		}

		private void Exited(object sender, EventArgs e)
		{
			ProcessExited?.Invoke(this, e);
		}

		private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			ErrorReceived?.Invoke(this, e.ToString());
		}

		private void ReceiveData()
		{
			while (true)
			{
				// Here we pray to the gods of cs that the data is only ever properly formatted

				// I wonder, what happens if the computer is slow?
				// Should I use a while loop like below and wait until all 8 bytes are read, then convert?
				// Is it possible that this function will ever break?
				// Just writing this as a helpful tip in case anyone ever needs to debug this line.
				// BitConverter.ToUInt64(buffer, 0);
				var usize = outHandle.ReadUInt64();

				var size = (int)usize;
				var num_read = 0;
				while (size - num_read != 0)
				{
					num_read += outHandle.Read(recvBuf, num_read, size - num_read);
				}

				var str = Encoding.UTF8.GetString(recvBuf, 0, size);

				DataReceived?.Invoke(this, str);
			}
		}
	}
}
