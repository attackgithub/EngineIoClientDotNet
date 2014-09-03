﻿//using log4net;

using EngineIoClientDotNet.Modules;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Quobject.EngineIoClientDotNet.Client;
using Quobject.EngineIoClientDotNet.Client.Transports;

using System.Collections.Immutable;
using System.Threading.Tasks;


namespace Quobject.EngineIoClientDotNet_Tests.ClientTests
{
    [TestClass]
    public class BinaryWebSocketTest : Connection
    {
        [TestMethod]
        public async Task ReceiveBinaryData()
        {
            LogManager.SetupLogManager();
            var log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod());
            log.Info("Start");

            var events = new ConcurrentQueue<object>();

            var binaryData = new byte[5];
            for (int i = 0; i < binaryData.Length; i++)
            {
                binaryData[i] = (byte) (i + 0);
            }


            var options = CreateOptions();

            var socket = new Socket(options);

            socket.On(Socket.EVENT_OPEN, () =>
            {
                log.Info(Socket.EVENT_OPEN);
                socket.On(Socket.EVENT_UPGRADE, () =>
                {

                    log.Info(Socket.EVENT_UPGRADE);
                    socket.Send(binaryData);
                });
            });

            socket.On(Socket.EVENT_MESSAGE, (d) =>
            {

                var data = d as string;
                log.Info(string.Format("EVENT_MESSAGE data ={0} d = {1} ", data, d));

                if (data == "hi")
                {
                    return;
                }
                events.Enqueue(d);
            });

            socket.Open();

            await Task.Delay(1000);

            log.Info("ReceiveBinaryData end");

            var binaryData2 = new byte[5];
            for (int i = 0; i < binaryData2.Length; i++)
            {
                binaryData2[i] = (byte) (i + 1);
            }

            object result;
            events.TryDequeue(out result);
            Assert.AreEqual(binaryData, result);
            socket.Close();
        }


        [TestMethod]
        public async Task ReceiveBinaryDataAndMultibyteUTF8String()
        {
            LogManager.SetupLogManager();
            var log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod());
            log.Info("Start");


            var events = new ConcurrentQueue<object>();

            var binaryData = new byte[5];
            for (int i = 0; i < binaryData.Length; i++)
            {
                binaryData[i] = (byte) i;
            }
            const string stringData = "cash money €€€";

            var options = CreateOptions();
            options.Transports = ImmutableList.Create<string>(Polling.NAME);


            var socket = new Socket(options);

            socket.On(Socket.EVENT_OPEN, () =>
            {

                log.Info("EVENT_OPEN");

                socket.Send(binaryData);
                socket.Send(stringData);

            });

            socket.On(Socket.EVENT_MESSAGE, (d) =>
            {

                var data = d as string;
                log.Info(string.Format("EVENT_MESSAGE data ={0} d = {1} ", data, d));

                if (data == "hi")
                {
                    return;
                }
                events.Enqueue(d);
                if (events.Count > 1)
                {
                    socket.Close();
                }
            });

            socket.Open();
            await Task.Delay(1000);
            //socket.Close();

            var binaryData2 = new byte[5];
            for (int i = 0; i < binaryData2.Length; i++)
            {
                binaryData2[i] = (byte) (i + 1);
            }

            object result;
            events.TryDequeue(out result);
            Assert.AreEqual(binaryData, result);
            events.TryDequeue(out result);
            Assert.AreEqual(stringData, (string) result);
            await Task.Delay(1000);
            log.Info("ReceiveBinaryDataAndMultibyteUTF8String end");
        }


    }
}
