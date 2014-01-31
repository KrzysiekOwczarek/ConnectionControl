using AddressLibrary;
using Packet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConnectionControl
{
    public partial class ConnectionControl : Form
    {

        delegate void SetTextCallback(string text);

        private Address myAddr;
        private Address myRCAddr;

        private int netNum;
        private int subNetNum;

        public class UserData
        {
            public string userName;
            public Address userAddr;
            public int userCap;
            public bool canReq;
            public List<NodeMapping> userMappings;
            public List<VirtualPath> possibleOutVPs;

            public UserData(string userName, Address userAddr, int userCap, bool canReq)
            {
                this.userName = userName;
                this.userAddr = userAddr;
                this.userCap = userCap;
                this.canReq = canReq;
                this.userMappings = new List<NodeMapping>();
                this.possibleOutVPs = new List<VirtualPath>();
            }
        }

        public class VirtualPath
        {
            public string destAddr;
            public string vpi;
            public List<int> vci;

            public VirtualPath(string destAddr, string vpi)
            {
                this.destAddr = destAddr;
                this.vpi = vpi;
                this.vci = new List<int>();
            }
        }

        public class NodeMapping
        {
            public string incomingAddr; //z MSG ROUTE
            public string outcomingAddr; //z MSG ROUTE
            public string incomingVP;
            public string incomingVC;
            public string outcomingVP;
            public string outcomingVC;
            public bool toSend;

            public int callId;

            public NodeMapping(string incomingAddr, string incomingVP, string incomingVC, string outcomingAddr, string outcomingVP, string outcomingVC, int callId)
            {
                this.incomingAddr = incomingAddr;
                this.outcomingAddr = outcomingAddr;
                this.incomingVP = incomingVP;
                this.incomingVC = incomingVC;
                this.outcomingVP = outcomingVP;
                this.outcomingVC = outcomingVC;
                this.callId = callId;
                this.toSend = true;
            }
        }

        private class ConnectionRequest
        {
            public string srcAddr; // 1 W PODSIECI
            public string destAddr; // GLOBAL DEST
            public int connId;

            public string prevCCAddr;
            public string nextCCAddr;

            public string outNodeAddr; //TRANZYT W OBECNEJ
            public string inNodeAddr; //TRANZYT W NASTEPNEJ

            public int outVP;
            public int outVC;

            public bool active;
            public bool established;
            public List<UserData> connNodes;


            public ConnectionRequest(string srcAddr, string destAddr, int connId)
            {
                this.srcAddr = srcAddr;
                this.destAddr = destAddr;
                this.connId = connId;

                prevCCAddr = "-";
                nextCCAddr = "-";
                outNodeAddr = "-";
                inNodeAddr = "-";

                active = true;
                established = false;
                connNodes = new List<UserData>();
            }
        }

        
        //dane chmury
        private IPAddress cloudAddress;        //Adres na którym chmura nasłuchuje
        private Int32 cloudPort;           //port chmury
        private IPEndPoint cloudEndPoint;
        private Socket cloudSocket;

        private Thread receiveThread;     //wątek służący do odbierania połączeń
        private Thread sendThread;        // analogicznie - do wysyłania

        private Queue _whatToSendQueue;
        private Queue whatToSendQueue;

        //strumienie
        private NetworkStream networkStream;
        //lista podlączonych klientow + wezlów
        private List<UserData> userList;

        //aktualnie obslugiwane polaczenie i lista wszystkich polaczen aktywnych i nieaktywnych ktore przewinely sie przez to CC
        private ConnectionRequest currConnection;
        private List<ConnectionRequest> myConnections;

        private bool isDebug;

        public bool isConnectedToCloud { get; private set; } // czy połączony z chmurą?
        
        public ConnectionControl()
        {
            userList = new List<UserData>();
            myConnections = new List<ConnectionRequest>();
            isConnectedToCloud = false;
            isDebug = true;
            _whatToSendQueue = new Queue();
            whatToSendQueue = Queue.Synchronized(_whatToSendQueue);
            InitializeComponent();
        }

        public void sender()
        {
            while (isConnectedToCloud)
            {
                //jeśli coś jest w kolejce - zdejmij i wyślij
                if (whatToSendQueue.Count != 0)
                {
                    SPacket _pck = (SPacket)whatToSendQueue.Dequeue();
                    BinaryFormatter bformatter = new BinaryFormatter();
                    bformatter.Serialize(networkStream, _pck);
                    networkStream.Flush();
                    String[] _argsToShow = _pck.getParames().ToArray();
                    String argsToShow = "";
                    foreach (String str in _argsToShow)
                    {
                        argsToShow += str + " ";
                    }
                    if (isDebug) SetText("Wysłano: " + _pck.getSrc() + ":" + _pck.getDest() + ":" + argsToShow);
                }
            }
        }

        /// <summary>
        /// wątek odbierający wiadomości z chmury
        /// </summary>
        public void receiver()
        {
            while (isConnectedToCloud)
            {
                BinaryFormatter bf = new BinaryFormatter();
                try
                {
                    SPacket receivedPacket = (Packet.SPacket)bf.Deserialize(networkStream);
                    if (isDebug) SetText("Odczytano:\n" + receivedPacket.ToString());
                    List<String> _msgList = receivedPacket.getParames();
                    Address _senderAddr;
                    if (Address.TryParse(receivedPacket.getSrc(), out _senderAddr))
                    {
                        //gdy logowanie się
                        if (_msgList[0] == "HELLO")
                        {
                            try
                            {
                                string usr = _msgList[1];
                                Address usrAddr = _senderAddr;
                                bool tempIsOk = true;
                                UserData tempUser = null;

                                //SPRAWDZA CZY TAKI JUZ JEST
                                foreach (UserData ud in userList)
                                {
                                    if ((ud.userName == usr || ud.userAddr.ToString() == _senderAddr.ToString()))
                                    {
                                        tempIsOk = false;
                                        tempUser = ud;
                                    }
                                }

                                if (tempIsOk)
                                {
                                    
                                    userList.Add(new UserData(usr, usrAddr, 6, true));

                                    List<string> userNames = new List<string>();
                                    foreach(UserData us in userList)
                                    {
                                        userNames.Add(us.userName);
                                    }

                                    BindingSource bs = new BindingSource();
                                    bs.DataSource = userNames;

                                    this.Invoke((MethodInvoker)delegate()
                                    {
                                        selectedUserBox.DataSource = bs;
                                    });

                                    SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), "OK");
                                    whatToSendQueue.Enqueue(pck);
                                
                                }
                                else
                                {
                                    SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), "NAME_OR_ADDR_TAKEN_BY " + tempUser.userName + " WITH_ADDR " + tempUser.userAddr);
                                    whatToSendQueue.Enqueue(pck);
                                }
                            }
                            catch
                            {
                                SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), "ERROR");
                                whatToSendQueue.Enqueue(pck);
                            }
                            //gdy żądanie listy klientów
                        }
                        else if (_msgList[0] == "REQ_CONN")
                        {

                            try
                            {
                                foreach (UserData u in userList)
                                {
                                    SPacket pck = new SPacket(myAddr.ToString(), u.userAddr.ToString(), "REQ_VPATHS");
                                    whatToSendQueue.Enqueue(pck);
                                }
                                
                            }
                            catch
                            {
                                SetText("Cos walnelo przy żadaniu VP");
                            }

                            //Z NCC
                            if (_senderAddr.ToString() == "0.0.2")
                            {
                                try
                                {
                                    string src = _msgList[1];
                                    string dest = _msgList[2];
                                    string connId = _msgList[3];

                                    currConnection  = new ConnectionRequest(src, dest, Convert.ToInt32(connId));
                                    currConnection.prevCCAddr = src;

                                    SPacket pck = new SPacket(myAddr.ToString(), myRCAddr.ToString(), "REQ_ROUTE " + src + " " + dest);
                                    whatToSendQueue.Enqueue(pck);

                                }
                                catch
                                {
                                    SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), "REQ_CONN z NCC ma za mało danych?");
                                    whatToSendQueue.Enqueue(pck);
                                }
                            }
                            // Z CC
                            else
                            {
                                try
                                {
                                    int connId = Convert.ToInt32(_msgList[1]);
                                    string incomingAddr = _msgList[2];
                                    string src = _msgList[3];
                                    string vp = _msgList[4];
                                    string vc = _msgList[5];
                                    string dest = _msgList[6];

                                    currConnection = new ConnectionRequest(src, dest, connId);

                                    UserData tempUser = null;
                                    bool userFound = false;

                                    foreach (UserData us in userList)
                                    {
                                        if (us.userAddr.ToString() == src)
                                        {
                                            tempUser = us;
                                            userFound = true;
                                            break;
                                        }
                                    }

                                    if(userFound)
                                    {
                                        //DODANE ALE NIE WYSŁANE, WYSYŁA DOPIERO PO OTRZYMANIU ROUTE OD RC BO TRZEBA DOKLEIC RESZTE MAPOWANIA
                                        tempUser.userMappings.Add(new NodeMapping(incomingAddr, vp, vc, "-", "-", "-", connId));
                                    }
                                    
                                    SPacket pck = new SPacket(myAddr.ToString(), myRCAddr.ToString(), "REQ_ROUTE " + src + " " + dest);
                                    whatToSendQueue.Enqueue(pck);

                                }
                                catch
                                {
                                    SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), "REQ_CONN z innego CC ma za mało danych?");
                                    whatToSendQueue.Enqueue(pck);
                                }
                            }

                        }
                        else if(_msgList[0] == "REQ_DISCONN")
                        {
                            int connId = Convert.ToInt32(_msgList[1]);
                            ConnectionRequest connToDis = null;

                            foreach(ConnectionRequest cr in myConnections)
                            {
                                if(cr.connId == connId && cr.active == true)
                                    connToDis = cr;
                            }

                            if (connToDis != null)
                            {
                                try
                                {
                                    foreach(UserData us in connToDis.connNodes)
                                    {
                                        foreach(NodeMapping nm in us.userMappings)
                                        {
                                            try
                                            {
                                                string msg;

                                                if (nm.outcomingAddr == "-" && nm.outcomingVP == "-" && nm.outcomingVC == "-")
                                                {
                                                    msg = "DEL_MAPPING " + nm.incomingAddr + " " + nm.incomingVP + " " + nm.incomingVC;
                                                }
                                                else
                                                {
                                                    msg = "DEL_MAPPING " + nm.incomingAddr + " " + nm.incomingVP + " " + nm.incomingVC + " " + nm.outcomingAddr + " " + nm.outcomingVP + " " + nm.outcomingVC;
                                                }
                                                
                                                SPacket pck = new SPacket(myAddr.ToString(), us.userAddr.ToString(), msg);
                                                whatToSendQueue.Enqueue(pck);


                                                deleteVirtualConnection(us, nm.outcomingVP, nm.outcomingVC, nm.outcomingAddr);
                                            }catch{
                                                SetText("Error sending DEL mapping");
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    SetText("Error whilst disconnecting connection");
                                }

                                if(connToDis.nextCCAddr != "-")
                                    try
                                    {
                                        SPacket pck = new SPacket(myAddr.ToString(), connToDis.nextCCAddr, "REQ_DISCONN " + connToDis.connId);
                                        whatToSendQueue.Enqueue(pck);

                                        connToDis.active = false;
                                    }
                                    catch
                                    {
                                        SetText("DEL NIE POSZEDL DO NEXTCC");
                                    }
                                else
                                {
                                    try
                                    {
                                        SPacket pck = new SPacket(myAddr.ToString(), "0.0.2", "CONN_DISCONN " + connToDis.connId);
                                        whatToSendQueue.Enqueue(pck);

                                        connToDis.active = false;
                                    }
                                    catch
                                    {
                                        SetText("NCC NIE OTRZYMAŁ POTWIERDZENIA DISCONN");
                                    }
                                }
                            }
                        }
                        else if(_msgList[0] == "ROUTE" && _senderAddr.ToString() == myRCAddr.ToString())
                        {
                            
                            try
                            {
                                UserData prevTempUser = null;

                                for (int i = 1; i < _msgList.Count(); i++ )
                                {
                                    UserData tempUser = null;
                                    NodeMapping foundMapping = null;
                                    NodeMapping newMapping = null;
                                    bool userFound = false;

                                    string s = _msgList[i];
                                    
                                    foreach(UserData us in userList)
                                    {
                                        if(us.userAddr.ToString() == s)
                                        {
                                            tempUser = us;
                                            userFound = true;
                                            break;
                                        }
                                    }

                                    
                                    try
                                    {
                                        string prev_s = null;
                                        string next_s = null;

                                        if(i > 0)
                                            prev_s = _msgList[i - 1];

                                        if((i+1) < _msgList.Count())
                                            next_s = _msgList[i + 1];

                                        if (!s.Contains('*'))
                                        {
                                            if (s.Contains(netNum + "." + subNetNum))
                                            {
                                                if (userFound)
                                                {
                                                    if (i == 1)
                                                    {
                                                        try
                                                        {
                                                            if (tempUser.userMappings.Count() != 0)
                                                            {
                                                                foreach (NodeMapping nm in tempUser.userMappings)
                                                                {
                                                                    if (nm.outcomingAddr == "-" && nm.incomingAddr != "-" && nm.toSend == true && nm.callId == currConnection.connId)
                                                                    {
                                                                        foundMapping = nm;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            SetText("Something wrong with CC mapping format [error while finding non-complete mapping but mapping are there...]");
                                                        }

                                                        try
                                                        {
                                                            if (foundMapping == null && tempUser.userAddr.ToString() != currConnection.destAddr)//DZWONIACY CLIENTX
                                                            {
                                                                string newVPI = nextVirtualPath(null, tempUser, next_s);
                                                                string newVCI = nextVirtualConnection(tempUser, newVPI, next_s);
                                                                newMapping = new NodeMapping(next_s, newVPI, newVCI, "-", "-", "-", currConnection.connId);
                                                            }
                                                            else if (foundMapping != null && tempUser.userAddr.ToString() != currConnection.destAddr) //PIERWSZY NODE W NOWEJ PODSIECI
                                                            {
                                                                string newVPI = nextVirtualPath(null, tempUser, next_s);
                                                                string newVCI = nextVirtualConnection(tempUser, newVPI, next_s);

                                                                foundMapping.outcomingAddr = next_s;
                                                                foundMapping.outcomingVP = newVPI;
                                                                foundMapping.outcomingVC = newVCI;
                                                            }
                                                            else if (foundMapping != null && tempUser.userAddr.ToString() == currConnection.destAddr)//PIERWSZY NODE W NOWEJ PODSIECI I OSTATNI W POŁĄCZENIU
                                                            {
                                                                NodeMapping prevMapping = prevTempUser.userMappings[prevTempUser.userMappings.Count() - 1];
                                                                newMapping = new NodeMapping(prev_s, prevMapping.outcomingVP, prevMapping.outcomingVC, "-", "-", "-", currConnection.connId);
                                                                currConnection.established = true;
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            SetText("Error while operating on first ROUTE command member...");
                                                        }
                                                    }//KONIEC PIERWSZEGO ADRESU Z ROUTE'A
                                                    else{
                                                        try
                                                        {
                                                            if (tempUser.userAddr.ToString() == currConnection.destAddr)//NIE PIERWSZY W ROUTE ALE OSTATNI W POŁĄCZENIU
                                                            {
                                                                try
                                                                {
                                                                    NodeMapping prevMapping = prevTempUser.userMappings[prevTempUser.userMappings.Count() - 1];

                                                                    string outvpi = null;

                                                                    if (prevMapping.outcomingVP == "-")
                                                                        outvpi = prevMapping.incomingVP;
                                                                    else
                                                                        outvpi = prevMapping.outcomingVP;

                                                                    string outvci = null;

                                                                    if (prevMapping.outcomingVP == "-")
                                                                        outvci = prevMapping.incomingVC;
                                                                    else
                                                                        outvci = prevMapping.outcomingVC;

                                                                    newMapping = new NodeMapping(prev_s, outvpi, outvci, "-", "-", "-", currConnection.connId);
                                                                    currConnection.established = true;
                                                                }
                                                                catch(Exception e)
                                                                {
                                                                    SetText("Error from not first in ROUTE but last in connection, node " + s + ", error: " + e.ToString());
                                                                }
                                                            }
                                                            else //NODIX GDZIEŚ POMIĘDZY, MA WEJŚCIE I WYJŚCIE
                                                            {
                                                                try
                                                                {
                                                                    NodeMapping prevMapping = prevTempUser.userMappings[prevTempUser.userMappings.Count() - 1];
                                                                    string newVPI = nextVirtualPath(prevMapping, tempUser, next_s);
                                                                    string newVCI = nextVirtualConnection(tempUser, newVPI, next_s);

                                                                    string outvpi = null;

                                                                    if (prevMapping.outcomingVP == "-")
                                                                        outvpi = prevMapping.incomingVP;
                                                                    else
                                                                        outvpi = prevMapping.outcomingVP;

                                                                    string outvci = null;

                                                                    if (prevMapping.outcomingVP == "-")
                                                                        outvci = prevMapping.incomingVC;
                                                                    else
                                                                        outvci = prevMapping.outcomingVC;

                                                                    newMapping = new NodeMapping(prev_s, outvpi, outvci, next_s, newVPI, newVCI, currConnection.connId);
                                                                }
                                                                catch(Exception e)
                                                                {
                                                                    SetText("Error from transit node: " + s + "Exception e: " + e.ToString());
                                                                }
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            SetText("Error while operating on not-first members of ROUTE command");
                                                        }
                                                    }
                                                    
                                                   
                                                    try
                                                    {
                                                        if (newMapping != null)
                                                        {
                                                            tempUser.userMappings.Add(newMapping);
                                                            prevTempUser = tempUser;
                                                        }
                                                        else if (foundMapping != null)
                                                        {
                                                            prevTempUser = tempUser;
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        SetText("Error while adding newMapping to current NODE");
                                                    }

                                                    currConnection.connNodes.Add(tempUser);
                                                    //TU ZAMKNIJ
                                                }
                                                else
                                                    SetText("Error while operating on ROUTE, there is no connected client having address: " + s);
                                            }
                                            else
                                            {
                                                //PIERWSZY NIE Z GWIAZDKA I OSTATNI W OGOLE CZYLI NODE Z NEXT SUBNET
                                                currConnection.outNodeAddr = prev_s;
                                                currConnection.inNodeAddr = s;
                                                NodeMapping prevMapping = prevTempUser.userMappings[prevTempUser.userMappings.Count() - 1];
                                                //string newVPI = nextVirtualPath(prevMapping, tempUser, next_s);
                                                //TU_VP
                                                currConnection.outVP = Convert.ToInt32(prevMapping.outcomingVP);
                                                currConnection.outVC = Convert.ToInt32(prevMapping.outcomingVC);
                                            }


                                        }
                                        else
                                        {
                                            //TU ZŁAPIE SIĘ TYLKO PIERWSZY Z GWIAZDKA I SUPER BO PRZEKSZTALCAMY GWIAZDKE NA 1 I MAMY NOWY CC DO KTOREGO WYSYLAMY REQ_CONN

                                            currConnection.nextCCAddr = s.Replace('*', '1');

                                            break;
                                        }
                                    }
                                    catch(Exception e)
                                    {
                                        SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), "Nie wyszło " + e.ToString());
                                        whatToSendQueue.Enqueue(pck);
                                    }
                                }

                                //ROZESLIJ WSZELKIE OCZEKUJACE MAPPINGI DO NODE'ÓW
                                sendMappingsToNodes();

                                if (currConnection.inNodeAddr != "-" && currConnection.outNodeAddr != "-" && currConnection.nextCCAddr != "-")
                                    sendToNextSubnet();

                                if(currConnection.established == true)
                                {
                                    SPacket pck = new SPacket(myAddr.ToString(), "0.0.2", "CONN_EST " + currConnection.connId);
                                    whatToSendQueue.Enqueue(pck);
                                }

                                myConnections.Add(currConnection);
                              
                            }
                            catch(Exception e)
                            {
                                SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), "ROUTE ERROR PODLACZYŁEŚ JAKIEŚ KLIENTY DEBILU? " + e.ToString());
                                whatToSendQueue.Enqueue(pck);
                            }
                        }
                        else if (_msgList[0] == "ROUTE_NOT_FOUND")
                        {

                        }
                        else if(_msgList[0] == "RES_VPATHS")
                        {
                            try
                            {
                                receiveVirtualPaths(_msgList, _senderAddr.ToString());
                            }
                            catch
                            {
                                SetText("Error whilst receiving VPATHS...");
                            }
                        }
                        else if(_msgList[0] == "DEAD")
                        {
                            string deadAddr = _msgList[1];

                            List<ConnectionRequest> toRestoreConns = new List<ConnectionRequest>();

                            foreach(ConnectionRequest cr in myConnections)
                            {
                                foreach(UserData ud in cr.connNodes)
                                {
                                    foreach (NodeMapping nm in ud.userMappings)
                                    {
                                        if (nm.incomingAddr.Equals(deadAddr) || nm.outcomingAddr.Equals(deadAddr))
                                        {
                                            var conn = from c in toRestoreConns where c.connId == cr.connId select c;

                                            if(!conn.Any())
                                                toRestoreConns.Add(cr);
                                        }
                                    }
                                }
                            }


                            try
                            {
                                foreach (ConnectionRequest cr in toRestoreConns)
                                {
                                    foreach (UserData us in cr.connNodes)
                                    {
                                        foreach (NodeMapping nm in us.userMappings)
                                        {
                                            try
                                            {
                                                string msg = "DEL_MAPPING " + nm.incomingAddr + " " + nm.incomingVP + " " + nm.incomingVC + " " + nm.outcomingAddr + " " + nm.outcomingVP + " " + nm.outcomingVC;
                                                SPacket pck = new SPacket(myAddr.ToString(), us.userAddr.ToString(), msg);
                                                whatToSendQueue.Enqueue(pck);


                                                deleteVirtualConnection(us, nm.outcomingVP, nm.outcomingVC, nm.outcomingAddr);
                                            }
                                            catch
                                            {
                                                SetText("Error sending DEL mapping in DEAD section");
                                            }
                                        }
                                    }

                                    try
                                    {
                                        string secParam = null;

                                        if(cr.outNodeAddr == "-")
                                        {
                                            secParam = cr.srcAddr;
                                        }
                                        else
                                        {
                                            secParam = cr.inNodeAddr;
                                        }

                                        //SPacket pck = new SPacket(myAddr.ToString(), myRCAddr.ToString(), "REQ_ROUTE " + cr.srcAddr + " " + secParam);
                                        SPacket pck = new SPacket(myAddr.ToString(), myRCAddr.ToString(), "REQ_CONNECT " + cr.connId + " " + cr.outNodeAddr + " " +
                                            cr.outVP + " " + cr.outVC + " " + cr.destAddr);
                                        whatToSendQueue.Enqueue(pck);

                                        receivedPacket = (Packet.SPacket)bf.Deserialize(networkStream);
                                        //if (isDebug) SetText("Odczytano:\n" + receivedPacket.ToString());
                                        List<String> _sub_msgList = receivedPacket.getParames();
                                        Address _sub_senderAddr;
                                        if (Address.TryParse(receivedPacket.getSrc(), out _sub_senderAddr))
                                        {
                                            if(_sub_msgList[0] == "ROUTE_NOT_FOUND")
                                            {
                                                /*if (cr.prevCCAddr != "-")
                                                {
                                                    try
                                                    {

                                                        pck = new SPacket(myAddr.ToString(), cr.prevCCAddr, "REQ_SRC " + cr.connId);
                                                        whatToSendQueue.Enqueue(pck);

                                                        receivedPacket = (Packet.SPacket)bf.Deserialize(networkStream);
                                                        //if (isDebug) SetText("Odczytano:\n" + receivedPacket.ToString());
                                                        List<String> _sub_sub_msgList = receivedPacket.getParames();
                                                        Address _sub_sub_senderAddr;
                                                        if (Address.TryParse(receivedPacket.getSrc(), out _sub_sub_senderAddr))
                                                        {
                                                            if (_sub_sub_msgList[0] == "MY_SRC")
                                                            {
                                                                string prevCCsource = _sub_sub_msgList[1];
                                                            }
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        SetText("Error whilst cross-domain restoration asking RC for cross-domain ROUTE");
                                                    }
                                                }
                                                else
                                                {
                                                    pck = new SPacket(myAddr.ToString(), "0.0.2", "CONN_NOEST " + cr.connId);
                                                    whatToSendQueue.Enqueue(pck);
                                                }*/
                                                //SetText("");
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        SetText("Error whilst sending request to RC for restoring in-domain route");
                                    }
                                }
                            }
                            catch
                            {
                                SetText("Error whilst disconnecting connection");
                            }


                            


                            //SetText("");

                            //DISCONNECT SASIEDNIE WEZLY ZMAPOWANE


                            //SPacket pck = new SPacket(myAddr.ToString(), "0.0.2", "REQ_CONN );
                            //whatToSendQueue.Enqueue(pck);
                        }
                        /*else if (_msgList[0] == "REQ_SRC")
                        {
                            int id = Convert.ToInt32(_msgList[1]);

                            var conn = from c in myConnections where c.connId == id select c;

                            if(conn.Any()){
                                foreach(var c in conn)
                                {
                                    SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), "MY_SRC " + c.srcAddr);
                                    whatToSendQueue.Enqueue(pck);
                                }
                            }
                        }*/
                    }
                }
                catch
                {
                    SetText("Coś poszło nie tak");
                }
            }
        }

        private void conToCloudButton_Click(object sender, EventArgs e)
        {
            if (!isConnectedToCloud)
            {
                //TO CHYBA NIE NAJELPSZE MIEJSCE NA SETRCADDRESS...
                if (setAddress() && setRCAddress())
                {
                    if (IPAddress.TryParse(cloudIPTextBox.Text, out cloudAddress))
                    {
                        SetText("IP ustawiono jako " + cloudAddress.ToString());
                    }
                    else
                    {
                        SetText("Błąd podczas ustawiania IP chmury (zły format?)");
                    }
                    if (Int32.TryParse(cloudPortTextBox.Text, out cloudPort))
                    {
                        SetText("Port chmury ustawiony jako " + cloudPort.ToString());
                    }
                    else
                    {
                        SetText("Błąd podczas ustawiania portu chmury (zły format?)");
                    }

                    cloudSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    cloudEndPoint = new IPEndPoint(cloudAddress, cloudPort);
                    try
                    {
                        cloudSocket.Connect(cloudEndPoint);
                        isConnectedToCloud = true;
                        networkStream = new NetworkStream(cloudSocket);
                        List<String> _welcArr = new List<String>();
                        _welcArr.Add("HELLO");
                        SPacket welcomePacket = new SPacket(myAddr.ToString(), new Address(0, 0, 0).ToString(), _welcArr);
                        whatToSendQueue.Enqueue(welcomePacket);
                        receiveThread = new Thread(this.receiver);
                        receiveThread.IsBackground = true;
                        receiveThread.Start();
                        sendThread = new Thread(this.sender);
                        sendThread.IsBackground = true;
                        sendThread.Start();
                        conToCloudButton.Text = "Rozłącz";
                        SetText("Połączono!");
                        SetText("RC adres: " + myRCAddr.ToString());
                    }
                    catch (SocketException)
                    {
                        isConnectedToCloud = false;
                        SetText("Błąd podczas łączenia się z chmurą");
                        SetText("Złe IP lub port? Chmura nie działa?");
                    }
                }
                else
                {
                    SetText("Wprowadź numery sieci i podsieci");
                }
            }
            else
            {
                isConnectedToCloud = false;
                //sendButton.Enabled = false;
                conToCloudButton.Text = "Połącz";
                SetText("Rozłączono!");
                if (cloudSocket != null) cloudSocket.Close();
            }
        }

        public string nextVirtualConnection(UserData currUser, string virtualPathId, string destAddr)
        {
            string newVCI = null;

            var vpi = from v in currUser.possibleOutVPs where (v.destAddr.Equals(destAddr) && v.vpi.Equals(virtualPathId)) select v;

            if(vpi.Any())
            {
                foreach (var v in vpi)
                {
                    newVCI = (v.vci.Count() + 1).ToString();
                    v.vci.Add(v.vci.Count()+1);
                }
            }
            else
            {
                SetText("Error whilst generating new VCI");
            }

            return newVCI;
        }

        public void deleteVirtualConnection(UserData currUser, string virtualPathId, string virtualConnectionId, string destAddr)
        {
            var vpi = from v in currUser.possibleOutVPs where (v.destAddr.Equals(destAddr) && v.vpi.Equals(virtualPathId)) select v;

            if (vpi.Any())
            {
                foreach (var v in vpi)
                {
                    v.vci.Remove(Convert.ToInt32(virtualConnectionId));
                }
            }
            else
            {
                SetText("Error whilst generating new VCI");
            }

        }

        public string nextVirtualPath(NodeMapping prevMapping, UserData currUser, string destAddr)
        {
            string nextVPI = null;
            Random random = new Random();

            //NodeMapping lastMapping = prevUser.userMappings[prevUser.userMappings.Count() - 1];
            if (prevMapping != null)
            {
                string outvpi = null;

                if (prevMapping.outcomingVP == "-")
                    outvpi = prevMapping.incomingVP;
                else
                    outvpi = prevMapping.outcomingVP;

                var vpi = from v in currUser.possibleOutVPs where (v.destAddr.Equals(destAddr) && v.vpi.Equals(outvpi)) select v; //ZJEBANE?
                
                if (vpi.Any())
                    nextVPI = outvpi;
                else
                {
                    var possiblevpi = from v in currUser.possibleOutVPs where v.destAddr.Equals(destAddr) select v;
                    List<VirtualPath> possiblePaths = possiblevpi.ToList();
                    nextVPI = (possiblePaths[random.Next(0, possiblePaths.Count() - 1)]).vpi;
                }
            }
            else
            {
                var possiblevpi = from v in currUser.possibleOutVPs where v.destAddr.Equals(destAddr) select v;
                List<VirtualPath> possiblePaths = possiblevpi.ToList();
                nextVPI = (possiblePaths[random.Next(0, possiblePaths.Count() - 1)]).vpi;
            }

            return nextVPI;
        }

        public void receiveVirtualPaths(List<string> msg, string sender)
        {
            try
            {
                List<string> _sub_msg = null;
                string myAddr = sender;

                for(int i = 1; i < msg.Count(); i++)
                {
                    _sub_msg = msg[i].Split('#').ToList();
                    var user = from u in userList where u.userAddr.ToString() == sender select u; 
                    
                  
                    foreach(var u in user)
                    {
                        string destAddr = _sub_msg[0];
                        for(int j = 1; j < _sub_msg.Count(); j++)
                        {
                            try
                            {
                                var vpi = from v in u.possibleOutVPs where (v.destAddr.Equals(destAddr) && v.vpi.Equals(_sub_msg[j])) select v;
                                if (!vpi.Any())
                                    u.possibleOutVPs.Add(new VirtualPath(destAddr, _sub_msg[j]));
                            }
                            catch
                            {
                                SetText("Error whilst checking if vpi is on...");
                            }

                            //SetText("Dodano VP o nr " + _sub_msg[j] + " na połączenie z adresem " + destAddr + " do usera o adresie " + u.userAddr.ToString());
                        }
                    }
                    
                }
            }
            catch
            {
                
            }
        }

        public void sendToNextSubnet()
        {
            string msg = "REQ_CONN " + currConnection.connId + " " + currConnection.outNodeAddr + " " + currConnection.inNodeAddr 
                + " " + currConnection.outVP + " " + currConnection.outVC + " " + currConnection.destAddr;

            SPacket pck = new SPacket(myAddr.ToString(), currConnection.nextCCAddr, msg);
            whatToSendQueue.Enqueue(pck);

        }

        public void sendMappingsToNodes()
        {
            foreach (UserData us in userList)
            {
                foreach (NodeMapping nodeMapping in us.userMappings)
                {
                    if (nodeMapping.toSend == true)
                        try
                        {
                            string msg = null;

                            

                            if (nodeMapping.outcomingAddr == "-" && nodeMapping.outcomingVP == "-" && nodeMapping.outcomingVC == "-")
                            {
                                /*var mapping = from m in us.userMappings
                                              where (m.incomingAddr.Equals(nodeMapping.incomingAddr) &&
                                                  m.incomingVP.Equals(nodeMapping.incomingVP) && m.incomingVC.Equals(nodeMapping.incomingVC) &&
                                                  m.outcomingAddr.Equals("-") && m.incomingVP.Equals("-") && m.incomingVC.Equals("-"))
                                              select m;
                                if(!mapping.Any())*/
                                    msg = "ADD_MAPPING " + nodeMapping.incomingAddr + " " + nodeMapping.incomingVP + " " + nodeMapping.incomingVC + " " + nodeMapping.callId;
                            }
                            else
                            {
                                /*var mapping = from m in us.userMappings
                                              where (m.incomingAddr.Equals(nodeMapping.incomingAddr) &&
                                                  m.incomingVP.Equals(nodeMapping.incomingVP) && m.incomingVC.Equals(nodeMapping.incomingVC) &&
                                                  m.outcomingAddr.Equals(nodeMapping.outcomingAddr) && m.outcomingVP.Equals(nodeMapping.outcomingVP) 
                                                  && m.outcomingVC.Equals(nodeMapping.outcomingVC))
                                              select m;

                                if(!mapping.Any())*/
                                    msg = "ADD_MAPPING " + nodeMapping.incomingAddr + " " + nodeMapping.incomingVP + " " + nodeMapping.incomingVC + " "
                                        + nodeMapping.outcomingAddr + " " + nodeMapping.outcomingVP + " " + nodeMapping.outcomingVC;
                            }

                            SPacket pck = new SPacket(myAddr.ToString(), us.userAddr.ToString(), msg);
                            whatToSendQueue.Enqueue(pck);

                            nodeMapping.toSend = false;
                        }
                        catch
                        {
                            SetText("Error while sending mappings...");
                        }
                }
            }
        }

        /// <summary>
        /// metoa ustalająca adres RC
        /// </summary>
        /// <returns>czy się udało czy nie</returns>
        public bool setAddress()
        {
            int _netNum;
            int _subnetNum;
            if (int.TryParse(networkNumberTextBox.Text, out _netNum))
                if (int.TryParse(subnetTextBox.Text, out _subnetNum))
                {
                    myAddr = new Address(_netNum, _subnetNum, 1);
                    netNum = _netNum;
                    subNetNum = _subnetNum;
                    return true;
                }
                else return false;
            else return false;
        }

        public bool setRCAddress()
        {
            int _netNum;
            int _subnetNum;
            if (int.TryParse(networkNumberTextBox.Text, out _netNum))
                if (int.TryParse(subnetTextBox.Text, out _subnetNum))
                {
                    myRCAddr = new Address(_netNum, _subnetNum, 0);
                    return true;
                }
                else return false;
            else return false;
        }

        public void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (this.log.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                try
                {
                    this.log.AppendText(text + "\n");
                    this.log.ScrollToCaret();
                }
                catch { }
            }
        }

    }
}
