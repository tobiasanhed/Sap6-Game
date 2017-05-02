﻿using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EngineName.Components;

namespace StandaloneNetworkApp
{
    public class Program
    {

        public class P2P
        {
            private NetPeer _peer;
            private NetPeerConfiguration _config;
            private NetClient _client;
            NetIncomingMessage _msg;
            public P2P()
            {
                _config = new NetPeerConfiguration("Sap6_Networking");
                _config.Port = 50002;
                _config.AcceptIncomingConnections = true;
                _config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
                _config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
                _config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
                _config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
                //Setup Connection First check if server is being hosted
                _peer = new NetPeer(_config);
                _peer.Start();
                _peer.DiscoverLocalPeers(50001);
                Console.WriteLine("listening on " + _config.Port.ToString());
            }
            /// <summary>Checks if have peers, so its possible to send stuff</summary>
            private bool havePeers()
            {
                return _peer.Connections != null && _peer.Connections.Count > 0;
            }
            public void SendObject(object data)
            {
                if (!havePeers())
                {
                    Debug.WriteLine("No connections to send to.");
                    return;
                }
                NetOutgoingMessage msg = _peer.CreateMessage();
                msg.Write((byte)Enums.MessageType.Object);
                msg.WriteAllProperties(data);
                _peer.SendMessage(msg, _peer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
            }
            /// <summary>Send information about newly connected peer to all other peers </summary>
            public void SendPeerInfo(IPAddress ip, int port)
            {
                if (!havePeers())
                {
                    Debug.WriteLine("No connections to send to.");
                    return;
                }
                Debug.WriteLine(string.Format("Broadcasting {0}:{1} to all (count: {2})", ip.ToString(),
                    port.ToString(), _peer.ConnectionsCount));
                NetOutgoingMessage msg = _peer.CreateMessage();
                msg.Write((int)Enums.MessageType.PeerInformation);
                byte[] addressBytes = ip.GetAddressBytes();
                msg.Write(addressBytes.Length);
                msg.Write(addressBytes);
                msg.Write(port);
                _peer.SendMessage(msg, _peer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
            }
            /// <summary>Send simple string to all peers </summary>
            public void SendMessage(string message)
            {
                if (!havePeers())
                {
                    Debug.WriteLine("No connections to send to.");
                    return;
                }
                NetOutgoingMessage msg = _peer.CreateMessage();
                msg.Write((int)Enums.MessageType.StringMessage);
                msg.Write(message);
                _peer.SendMessage(msg, _peer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
            }
            public void loop()
            {
                NetIncomingMessage msg;
                while ((msg = _peer.ReadMessage()) != null)
                {
                    switch (msg.MessageType)
                    {
              
                        case NetIncomingMessageType.DiscoveryRequest:
                            Console.WriteLine("ReceivePeersData DiscoveryRequest");
                            _peer.SendDiscoveryResponse(null, msg.SenderEndPoint);
                            break;
                        case NetIncomingMessageType.DiscoveryResponse:
                            // just connect to first server discovered
                            Console.WriteLine("ReceivePeersData DiscoveryResponse CONNECT");
                            _peer.Connect(msg.SenderEndPoint);
                            break;
                        case NetIncomingMessageType.ConnectionApproval:
                            Console.WriteLine("ReceivePeersData ConnectionApproval");
                            msg.SenderConnection.Approve();
                            //broadcast this to all connected clients
                            Console.WriteLine("Sending PeerInfo");
                            SendPeerInfo(msg.SenderEndPoint.Address, msg.SenderEndPoint.Port);
                            break;
                        case NetIncomingMessageType.Data:
                            //another client sent us data
                            Console.WriteLine("BEGIN ReceivePeersData Data");
                            Enums.MessageType mType = (Enums.MessageType)msg.ReadInt32();
                            if (mType == Enums.MessageType.StringMessage)
                            {
                                Console.WriteLine("Message From" + msg.SenderEndPoint.Address+ " "  + msg.ReadString());
                            }
                            else if (mType == Enums.MessageType.PeerInformation)
                            {
                                int byteLenth = msg.ReadInt32();
                                byte[] addressBytes = msg.ReadBytes(byteLenth);
                                IPAddress ip = new IPAddress(addressBytes);
                                int port = msg.ReadInt32();
                                //connect
                                IPEndPoint endPoint = new IPEndPoint(ip, port);
                                if (_peer.GetConnection(endPoint) == null)
                                {
//are we already connected?
                                    //Don't try to connect to ourself!
                                    if (_peer.Configuration.LocalAddress.GetHashCode() != endPoint.Address.GetHashCode()
                                        || _peer.Configuration.Port.GetHashCode() != endPoint.Port.GetHashCode())
                                    {
                                        Console.WriteLine(
                                            string.Format("Data::PeerInfo::Initiate new connection to: {0}:{1}",
                                                endPoint.Address.ToString(), endPoint.Port.ToString()));
                                        _peer.Connect(endPoint);
                                    }
                                }
                            }
                            Console.WriteLine("END ReceivePeersData Data");
                            break;
                        case NetIncomingMessageType.UnconnectedData:
                            string orphanData = msg.ReadString();
                            Console.WriteLine("UnconnectedData: " + orphanData);
                            break;
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            Console.WriteLine(msg.ReadString());
                            break;
                        default:
                            Console.WriteLine("ReceivePeersData Unknown type: " + msg.MessageType.ToString());
                            try
                            {
                                Console.WriteLine(msg.SenderConnection.Status);
                                Console.WriteLine(msg.ReadString());
                            }
                            catch
                            {
                                Console.WriteLine("Couldn't parse unknown to string.");
                            }
                            break;
                    }
                }
            }

            public static void Main(string[] args)
            {
                var stuff = new P2P();
                int counter = 0;
                while (true)
                {
                    counter++;
                    Thread.Sleep(1000);
                    stuff.loop();
                    if (counter % 5 == 0 && counter != 0)
                    {
                        counter++;
                        stuff.SendMessage("hej" + counter);
                        stuff.SendObject(new CTransform());
                    }
                    
                }
                

            }
        }
    }
}
