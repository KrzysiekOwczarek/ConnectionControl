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

        private class userData
        {
            public string userName;
            public Address userAddr;
            public int userCap;
            public bool canReq;
            public String userType;

            public userData(string userName, Address userAddr, int userCap, bool canReq, string userType)
            {
                this.userName = userName;
                this.userAddr = userAddr;
                this.userCap = userCap;
                this.canReq = canReq;
                this.userType = userType;
            }
        }


        //DODAC INDEXAMI MAPPINGI DO ZALOGOWANYCH WEZŁÓW?
        private class NodeMapping
        {
            public string nodeAddr;
            public string incomingAddr; //z MSG ROUTE
            public string outcomingAddr; //z MSG ROUTE
            public int incomingVP;
            public int incomingVC;
            public int outcomingVP;
            public int outcomingVC;

            public NodeMapping(string nodeAddr, string incomingAddr, string outcomingAddr, int incomingVP, int incomingVC, int outcomingVP, int outcomingVC)
            {
                this.nodeAddr = nodeAddr;
                this.incomingAddr = incomingAddr;
                this.outcomingAddr = outcomingAddr;
                this.incomingVP = incomingVP;
                this.incomingVC = incomingVC;
                this.outcomingVP = outcomingVP;
                this.outcomingVC = outcomingVC;
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
        private List<userData> userList;

        private bool isDebug;

        public bool isConnectedToCloud { get; private set; } // czy połączony z chmurą?
        
        public ConnectionControl()
        {
            userList = new List<userData>();
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
                        if (_msgList[0] == "LOGIN")
                        {
                            try
                            {
                                string usr = _msgList[1];
                                string type = _msgList[2];
                                Address usrAddr = _senderAddr;
                                bool tempIsOk = true;
                                userData tempUser = null;

                                //SPRAWDZA CZY TAKI JUZ JEST
                                foreach (userData ud in userList)
                                {
                                    if ((ud.userName == usr || ud.userAddr.ToString() == _senderAddr.ToString()) && ud.userType.Equals(type))
                                    {
                                        tempIsOk = false;
                                        tempUser = ud;
                                    }
                                }

                                if (tempIsOk)
                                {
                                    if (type.Equals("transport"))
                                        userList.Add(new userData(usr, usrAddr, 6, true, "transport"));
                                    else  if(type.Equals("client"))
                                        userList.Add(new userData(usr, usrAddr, 6, true, "client"));

                                    List<string> tempTransport = new List<string>();
                                    List<string> tempClient = new List<string>();
                                    foreach (userData ud in userList)
                                    {
                                        if(ud.userType.Equals("transport"))
                                            tempTransport.Add(ud.userName);
                                        if (ud.userType.Equals("client"))
                                            tempClient.Add(ud.userName);
                                    }


                                    BindingSource bsTransport = new BindingSource();
                                    bsTransport.DataSource = tempTransport;

                                    BindingSource bsClient = new BindingSource();
                                    bsClient.DataSource = tempClient;

                                    this.Invoke((MethodInvoker)delegate()
                                    {
                                        selectedClientBox.DataSource = bsClient;
                                        selectedTransportBox.DataSource = bsTransport;
                                    });

                                    SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), "OK");
                                    whatToSendQueue.Enqueue(pck);
                                    this.Invoke((MethodInvoker)delegate()
                                    {
                                        //selectedClientBox_SelectedIndexChanged();
                                    });
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
                                string src = _msgList[1];
                                string dest = _msgList[2];

                                //COS Z TYM TRZEBA ZROBIC, MYSL CHUJU.
                                string connId = _msgList[3];

                                SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), "REQ_ROUTE " + src + " " + dest);
                                whatToSendQueue.Enqueue(pck);
                                
                            }
                            catch
                            {
                                SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), "REQ_CONN ERROR");
                                whatToSendQueue.Enqueue(pck);
                            }

                        }
                        else if(_msgList[0] == "ROUTE")
                        {
                            List<NodeMapping> myNodeMappings = new List<NodeMapping>();

                            try
                            {
                                for (int i = 1; i < _msgList.Count(); i++ )
                                {
                                    string s = _msgList[i];
                                    string prev_s = _msgList[i - 1];
                                    string next_s = _msgList[i + 1];

                                    if (!s.Contains('*'))
                                    {
                                        if (s.Contains(netNum + "." + subNetNum))
                                        {
                                            if (i == 1)
                                                myNodeMappings.Add(new NodeMapping(s, null, next_s, 1, 1, 1, 1));
                                            else
                                                myNodeMappings.Add(new NodeMapping(s, prev_s, next_s, 1, 1, 1, 1));
                                        }
                                    }
                                    else
                                        break;
                                }

                                foreach (NodeMapping nodeMapping in myNodeMappings)
                                {
                                    try
                                    {
                                        string msg;

                                        if(nodeMapping.incomingAddr == null)
                                            msg = "ADD_MAPPING " + nodeMapping.outcomingAddr + " " + nodeMapping.outcomingVP + " " + nodeMapping.outcomingVC;
                                        else
                                            msg = "ADD_MAPPING " + nodeMapping.incomingAddr + " " + nodeMapping.incomingVP + " " + nodeMapping.incomingVC + " "
                                                + nodeMapping.outcomingAddr + " " + nodeMapping.outcomingVP + " " + nodeMapping.outcomingVC;

                                        //DODAJ MAPPINGS DO KLIENTOW/TRANSPORTOW NA LISCIE I STAMTAD ADRESY A NIE Z DUPY 
                                        SPacket pck = new SPacket(myAddr.ToString(), nodeMapping.nodeAddr, msg );
                                        whatToSendQueue.Enqueue(pck);
                                    }
                                    catch
                                    {
                                        SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), "REQ_CONN ERROR");
                                        whatToSendQueue.Enqueue(pck);
                                    }
                                }

                              
                            }
                            catch
                            {
                                SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), "ROUTE ERROR");
                                whatToSendQueue.Enqueue(pck);
                            }
                        }
                        /*else if (_msgList[0] == "REQ_CLIENTS")
                        {
                            List<string> clients = new List<string>();
                            clients.Add("CLIENTS");
                            String callerName = String.Empty;
                            foreach (userData ud in userList)
                            {
                                if (ud.userAddr == _senderAddr)
                                {
                                    callerName = ud.userName;
                                }
                            }
                            foreach (userData ud in userList)
                            {
                                if (ud.userName != callerName)
                                {
                                    clients.Add(ud.userName);
                                }
                            }
                            SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), clients);
                            whatToSendQueue.Enqueue(pck);
                            //gdy żądanie połączenia
                        }
                        else if (_msgList[0] == "REQ_CALL")
                        {
                            try
                            {
                                bool canCall = false;
                                foreach (userData ud in userList)
                                {
                                    if (ud.userAddr.ToString() == _senderAddr.ToString())
                                    {
                                        if (ud.canReq && int.Parse(_msgList[2]) <= ud.userCap) canCall = true;
                                    }
                                }
                                List<string> response = new List<string>();
                                if (canCall)
                                {
                                    foreach (userData ud in userList)
                                    {
                                        if (ud.userName != _msgList[1])
                                        {
                                            response.Add("YES");
                                            response.Add(ud.userAddr.ToString());
                                        }
                                    }
                                }
                                else response.Add("NO");
                                SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), response);
                                whatToSendQueue.Enqueue(pck);
                            }
                            catch
                            {
                                SPacket pck = new SPacket(myAddr.ToString(), _senderAddr.ToString(), "ERROR");
                                whatToSendQueue.Enqueue(pck);
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
                        //writer = new StreamWriter(networkStream);
                        //reader = new StreamReader(networkStream);
                        //sendButton.Enabled = true;
                        List<String> _welcArr = new List<String>();
                        _welcArr.Add("HELLO");
                        SPacket welcomePacket = new SPacket(myAddr.ToString(), new Address(0, 0, 0).ToString(), _welcArr);
                        whatToSendQueue.Enqueue(welcomePacket);
                        //whatToSendQueue.Enqueue("HELLO " + myAddr);
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
                }
                catch { }
            }
        }

    }
}
