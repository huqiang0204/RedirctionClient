using huqiang;
using huqiang.Data;
using MessagePack.LZ4;
using MsgPack.Serialization;
using Redirection.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RedirctionClient.DataControll
{
    [Serializable]
    public class ServerInfo
    {
        public int ip;
        public int port;
        public string name;
    }
    public class KcpDataControll
    {
        class KcpData
        {
            public byte[] dat;
            public byte tag;
        }
        class KcpSocket : KcpLink
        {
            public Queue<KcpData> datas;
            public KcpSocket(KcpServer server) : base(server)
            {
                datas = new Queue<KcpData>();
                Console.WriteLine("new link");
            }
            public override void Dispatch(byte[] dat, byte tag)
            {
                //KcpData data = new KcpData();
                //data.dat = dat;
                //data.tag = tag;
                //lock (datas)
                //    datas.Enqueue(data);
                KcpDataControll.Instance.DispatchEx(dat,tag);
            }
            public override void Disconnect()
            {

            }
            public override void ConnectionOK()
            {
                //LoginTable login = new LoginTable();
                //login.user = "hujianhai";
                //login.pass = "123456";
                //Instance.SendObject<LoginTable>(DefCmd.Login,MessageType.Def,login);
            }
            public void SendObject<T>(Int32 cmd, Int32 type, object obj) where T : class
            {
                DataBuffer db = new DataBuffer(4);
                var fake = new FakeStruct(db, Req.Length);
                fake[Req.Cmd] = cmd;
                fake[Req.Type] = type;
                db.fakeStruct = fake;
                var ser = MessagePackSerializer.Get<T>();
                MemoryStream ms = new MemoryStream();
                ser.Pack(ms, obj);
                fake.SetData(Req.Args, ms.ToArray());
                ms.Dispose();
                var dat = db.ToBytes();
                byte[] buf = new byte[dat.Length];
                int len = LZ4Codec.Encode32Unsafe(dat, 0, dat.Length, buf, 0, buf.Length);
                dat = WriteLen(dat.Length, len, buf);
                buf = AES.Instance.Encrypt(dat);
                Send(buf, EnvelopeType.AesDataBuffer);
            }
            byte[] WriteLen(int all, int len, byte[] dat)
            {
                var tmp = new byte[dat.Length + 8];
                var l = len.ToBytes();
                var a = all.ToBytes();
                int s = 4;
                for (int i = 0; i < 4; i++)
                {
                    tmp[i] = l[i];
                    tmp[s] = a[i];
                    s++;
                }
                for (int i = 0; i < len; i++)
                {
                    tmp[s] = dat[i];
                    s++;
                }
                return tmp;
            }
            public void SendNull(Int32 cmd, Int32 type)
            {
                DataBuffer db = new DataBuffer(4);
                var fake = new FakeStruct(db, Req.Length);
                fake[Req.Cmd] = cmd;
                fake[Req.Type] = type;
                db.fakeStruct = fake;
                var dat = db.ToBytes();
                byte[] buf = new byte[dat.Length];
                int len = LZ4Codec.Encode32Unsafe(dat, 0, dat.Length, buf, 0, buf.Length);
                dat = WriteLen(dat.Length, len, buf);
                buf = AES.Instance.Encrypt(dat);
                Send(buf, EnvelopeType.AesDataBuffer);
            }
        }
        static KcpDataControll ins;
        public static KcpDataControll Instance { get { if (ins == null) ins = new KcpDataControll(); return ins; } }
        string UniId;
        KcpSocket link;
        public void Connection(string ip, int port)
        {
            var address = IPAddress.Parse(ip);
            KcpServer.CreateLink = (o) => {
                return new KcpSocket(o);
            };
            var kcp = new KcpServer(0, 0, 1);
            link = kcp.CreateNewLink(new IPEndPoint(address, port)) as KcpSocket;
            link.SendNull(ProCmd.ServerIp,MessageType.Pro);
            //LoginTable login = new LoginTable();
            //login.user = "hujianhai";
            //login.pass = "123456";
            //Instance.SendObject<LoginTable>(DefCmd.Login, MessageType.Def, login);
        }
        public void Login()
        {
          
        }
        public int pin;
        public int userId;
        public void FailedConnect()
        {

        }
        public void Close()
        {
            if (KcpServer.Instance != null)
                KcpServer.Instance.Dispose();
        }
        public void DispatchMessage()
        {
            try
            {
                if (link != null)
                {
                    lock (link.datas)
                    {
                        int c = link.datas.Count;
                        for (int i = 0; i < c; i++)
                        {
                            var dat = link.datas.Dequeue();
                            DispatchEx(dat.dat, dat.tag);
                        }
                    }
                }
            }
            catch
            {
            }
        }
        float Time;
        void DispatchEx(byte[] data, byte tag)
        {
            byte type = tag;
            switch (type)
            {
                case EnvelopeType.Mate:
                    DispatchMetaData(data);
                    break;
                case EnvelopeType.AesJson:
                    byte[] dec = AES.Instance.Decrypt(data, 0, data.Length);
                    var json = Encoding.UTF8.GetString(dec);
                    DispatchJson(json);
                    break;
                case EnvelopeType.Json:
                    json = Encoding.UTF8.GetString(data);
                    DispatchJson(json);
                    break;
                case EnvelopeType.AesDataBuffer:
                    var dat = AES.Instance.Decrypt(data, 0, data.Length);
                    int len = dat.ReadInt32(0);
                    int all = dat.ReadInt32(4);
                    byte[] buf = new byte[all];
                    int o = LZ4Codec.Decode32Unsafe(dat, 8, len, buf, 0, all);
                    dat = new byte[o];
                    for (int i = 0; i < o; i++)
                        dat[i] = buf[i];
                    DispatchStream(new DataBuffer(dat));
                    break;
                case EnvelopeType.DataBuffer:
                    var buff = new DataBuffer(data);
                    DispatchStream(buff);
                    break;
                case EnvelopeType.String:
                    json = Encoding.UTF8.GetString(data);
                    DispatchString(json);
                    break;
            }
        }
        void DispatchMetaData(byte[] data)
        {

        }
        void DispatchString(string json)
        {

        }
        void DispatchJson(string json)
        {
        

        }
        void DispatchStream(DataBuffer buffer)
        {
            var fake = buffer.fakeStruct;
            if (fake != null)
            {
                switch (fake[Req.Type])
                {
                    case MessageType.Def:
                        //DefaultDataControll.Dispatch(buffer);
                        break;
                    case MessageType.Rpc:
                        //RpcData.Dispatch(linker, buffer);
                        break;
                    case MessageType.Query:
                        //QueryData.Dispatch(linker, buffer);
                        break;
                    case MessageType.Pro:
                        MessagePro(buffer);
                        break;
                }
            }
        }
        void SendAesJson(byte[] dat)
        {
            link.Send(dat, EnvelopeType.AesJson);
        }
        public void SendStream(DataBuffer db)
        {
            link.Send(db.ToBytes(), EnvelopeType.DataBuffer);
        }
        public void SendAesStream(DataBuffer db)
        {
            link.Send(AES.Instance.Encrypt(db.ToBytes()), EnvelopeType.AesDataBuffer);
        }
        public void SendString(Int32 cmd, Int32 type, string obj)
        {
            DataBuffer db = new DataBuffer(4);
            var fake = new FakeStruct(db, Req.Length);
            fake[Req.Cmd] = cmd;
            fake[Req.Type] = type;
            db.fakeStruct = fake;
            fake.SetData(Req.Args, obj);
            var dat = db.ToBytes();
            byte[] buf = new byte[dat.Length];
            int len = LZ4Codec.Encode32Unsafe(dat, 0, dat.Length, buf, 0, buf.Length);
            dat = WriteLen(dat.Length, len, buf);
            buf = AES.Instance.Encrypt(dat);
            link.Send(buf, EnvelopeType.AesDataBuffer);
        }
        public void SendObject<T>(Int32 cmd, Int32 type, object obj) where T : class
        {
            DataBuffer db = new DataBuffer(4);
            var fake = new FakeStruct(db, Req.Length);
            fake[Req.Cmd] = cmd;
            fake[Req.Type] = type;
            db.fakeStruct = fake;
            var ser = MessagePackSerializer.Get<T>();
            MemoryStream ms = new MemoryStream();
            ser.Pack(ms, obj);
            fake.SetData(Req.Args, ms.ToArray());
            ms.Dispose();
            var dat = db.ToBytes();
            byte[] buf = new byte[dat.Length];
            int len = LZ4Codec.Encode32Unsafe(dat, 0, dat.Length, buf, 0, buf.Length);
            dat = WriteLen(dat.Length, len, buf);
            buf = AES.Instance.Encrypt(dat);
            link.Send(buf, EnvelopeType.AesDataBuffer);
        }
        byte[] WriteLen(int all, int len, byte[] dat)
        {
            var tmp = new byte[dat.Length + 8];
            var l = len.ToBytes();
            var a = all.ToBytes();
            int s = 4;
            for (int i = 0; i < 4; i++)
            {
                tmp[i] = l[i];
                tmp[s] = a[i];
                s++;
            }
            for (int i = 0; i < len; i++)
            {
                tmp[s] = dat[i];
                s++;
            }
            return tmp;
        }
        void MessagePro(DataBuffer data)
        {
            switch (data.fakeStruct[Req.Cmd])
            {
                case ProCmd.ServerIp:
                    byte[] dat = data.fakeStruct.GetData<byte[]>(Req.Args);
                    var rs = KcpPack.UnPackMsg<ServerInfo>(dat);
                    if (rs != null)
                    {
                        link.Redirect(rs.ip, rs.port);
                        link.SendNull(DefCmd.heart, MessageType.Def);
                    }
                    break;
            }
        }
    }
}
