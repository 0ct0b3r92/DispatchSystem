﻿using System;
using System.Linq;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;

using Config.Reader;
using DispatchSystem.Common.DataHolders.Storage;
using DispatchSystem.Common.NetCode;

using CitizenFX.Core;

namespace DispatchSystem.sv.External
{
    public class Server
    {
        TcpListener tcp;
        iniconfig cfg;
        Permissions perms;
        int port;
        int Port => port;

        internal Server(iniconfig cfg)
        {
            this.cfg = cfg;
            perms = Permissions.Get;
            port = this.cfg.GetIntValue("server", "port", 33333);
            Log.WriteLine("Setting port to " + port.ToString());
            Start();
        }

        public void Start()
        {
            Log.WriteLine("Creating TCP Host");
            tcp = new TcpListener(IPAddress.Parse(cfg.GetStringValue("server", "ip", "0.0.0.0")), port);
            Log.WriteLine("Setting TCP connection IP to " + tcp.LocalEndpoint.ToString().Split(':')[0]);

            Log.WriteLine("TCP Created, Attempting to start");
            try { tcp.Start(); }
            catch (SocketException)
            {
                Log.WriteLine("The specified port (" + port + ") is already in use.");
                return;
            }
            Log.WriteLine("TCP Started, Listening for connections...");

            while (true)
                ThreadPool.QueueUserWorkItem(x => {
                    try { Connect(x); }
                    catch (Exception e)
                    {
                        Log.WriteLineSilent(e.ToString());
                    }
                }, tcp.AcceptSocket());
        }
        private async void Connect(object socket0)
        {
#if DEBUG
            Log.WriteLine($"New connection from ip");
#else
            Log.WriteLineSilent($"New connection from ip");
#endif
            NetRequestHandler net = new NetRequestHandler((Socket)socket0);

            net.Functions.Add("GetCivilian", new NetFunction(GetCivilian));
            net.Functions.Add("GetCivilianVeh", new NetFunction(GetCivilianVeh));
            net.Functions.Add("GetBolos", new NetFunction(GetBolos));
            net.Functions.Add("GetOfficers", new NetFunction(GetOfficers));
            net.Functions.Add("GetOfficer", new NetFunction(GetOfficer));
            net.Events.Add("SetStatus", new NetEvent(ChangeOfficerStatus));
            net.Events.Add("RemoveOfficer", new NetEvent(RemoveOfficer));
            net.Events.Add("AddBolo", new NetEvent(AddBolo));
            net.Events.Add("RemoveBolo", new NetEvent(RemoveBolo));
            net.Events.Add("AddNote", new NetEvent(AddNote));

            await net.Receive();

#if DEBUG
            Log.WriteLine($"Connection from ip broken");
#else
            Log.WriteLineSilent($"Connection from ip broken");
#endif
        }

        private async Task<object> GetCivilian(NetRequestHandler sender, object[] args)
        {
            await Task.FromResult(0);
            if (CheckAndDispose(sender))
                return null;
#if DEBUG
            Log.WriteLine("Get civilian Request Recieved");
#else
            Log.WriteLineSilent("Get civilian Request Recieved");
#endif

            string first = (string)args[0];
            string last = (string)args[1];

            Civilian civ = Common.GetCivilianByName(first, last);
            if (civ != null)
            {
#if DEBUG
                Log.WriteLine("Sending Civilian information to Client");
#else
                Log.WriteLineSilent("Sending Civilian information to Client");
#endif
                return civ;
            }
            else
            {
#if DEBUG
                Log.WriteLine("Civilian not found, sending null");
#else
                Log.WriteLineSilent("Civilian not found, sending null");
#endif
                return Civilian.Empty;
            }
        }
        private async Task<object> GetCivilianVeh(NetRequestHandler sender, object[] args)
        {
            await Task.FromResult(0);
            if (CheckAndDispose(sender))
                return null;
#if DEBUG
            Log.WriteLine("Get civilian veh Request Recieved");
#else
            Log.WriteLineSilent("Get civilian veh Request Recieved");
#endif

            string plate = (string)args[0];

            CivilianVeh civVeh = Common.GetCivilianVehByPlate(plate);
            if (civVeh != null)
            {
#if DEBUG
                Log.WriteLine("Sending Civilian Veh information to Client");
#else
                Log.WriteLineSilent("Sending Civilian Veh information to Client");
#endif
                return civVeh;
            }
            else
            {
#if DEBUG
                Log.WriteLine("Civilian Veh not found, sending null");
#else
                Log.WriteLineSilent("Civilian Veh not found, sending null");
#endif
                return CivilianVeh.Empty;
            }
        }
        private async Task<object> GetBolos(NetRequestHandler sender, object[] args)
        {
            await Task.FromResult(0);
            if (CheckAndDispose(sender))
                return null;
#if DEBUG
            Log.WriteLine("Get bolos Request Recieved");
#else
            Log.WriteLineSilent("Get bolos Request Recieved");
#endif

            return DispatchSystem.ActiveBolos;
        }
        private async Task<object> GetOfficers(NetRequestHandler sender, object[] args)
        {
            await Task.FromResult(0);
            if (CheckAndDispose(sender))
                return null;

#if DEBUG
            Log.WriteLine("Get officers Request Received");
#else
            Log.WriteLineSilent("Get officers Request Received");
#endif

            return DispatchSystem.officers;
        }
        private async Task<object> GetOfficer(NetRequestHandler sender, object[] args)
        {
            await Task.FromResult(0);
            if (CheckAndDispose(sender))
                return null;

            string ip = (string)args[0];

#if DEBUG
            Log.WriteLine("Get officer Request Received");
#else
            Log.WriteLineSilent("Get officer Request Received");
#endif

            Officer ofc = DispatchSystem.officers.ToList().Find(x => x.SourceIP == ip);
            ofc = ofc ?? Officer.Empty;
            return ofc;
        }
        private async Task ChangeOfficerStatus(NetRequestHandler sender, object[] args)
        {
            await Task.FromResult(0);
            if (CheckAndDispose(sender))
                return;

#if DEBUG
            Log.WriteLine("Change officer status Request Received");
#else
            Log.WriteLineSilent("Change officer status Request Received");
#endif

            int index = 0;
            Officer ofc = (Officer)args[0];
            OfficerStatus status = (OfficerStatus)args[1];

            try
            {
                ofc = DispatchSystem.officers.ToList().Find(x => x.SourceIP == ofc.SourceIP);
                index = DispatchSystem.officers.IndexOf(ofc);
                if (index == -1)
                    throw new IndexOutOfRangeException("The index was out of range!");
            }
            catch (IndexOutOfRangeException)
            {
#if DEBUG
                Log.WriteLine("Officer not found, aborting...");
#else
                Log.WriteLineSilent("Officer not found, aborting...");
#endif

                return;
            }

            Officer ourOfc = DispatchSystem.officers[index];

            if (ourOfc.Status != status)
            {
                ourOfc.Status = status;
#if DEBUG
                Log.WriteLine("Setting officer status to " + status.ToString());
#else
                Log.WriteLineSilent("Setting officer status to " + status.ToString());
#endif
            }
            else
            {
#if DEBUG
                Log.WriteLine("Officer status already set to the incoming status");
#else
                Log.WriteLineSilent("Officer status already set to the incoming status");
#endif
            }
        }
        private async Task RemoveOfficer(NetRequestHandler sender, object[] args)
        {
            await Task.FromResult(0);
            if (CheckAndDispose(sender))
                return;

#if DEBUG
            Log.WriteLine("Remove officer Request Received");
#else
            Log.WriteLineSilent("Add bolo Request Received");
#endif

            Officer ofcGiven = (Officer)args[0];

            Officer ofc = DispatchSystem.officers.ToList().Find(x => x.SourceIP == ofcGiven.SourceIP);
            if (ofc != null)
            {
                /*
                try
                {
                    Player p = new PlayerList().ToList().Find(x => x.Identifiers["ip"] == ofc.SourceIP); // Crashes at this

                    if (p != null)
                    {
                        BaseScript.TriggerClientEvent(p, "chatMessage", "DispatchSystem", new[] { 0,0,0}, "test");
                    }
                }
                catch (Exception e)
                {
                    Log.WriteLine(e.ToString());
                }
                */

                // Adding a item to execute on the main thread
                try
                {
                    DispatchSystem.Invoke(delegate
                    {
                        Player p = Common.GetPlayerByIp(ofc.SourceIP);

                        if (p != null)
                        {
                            Common.SendMessage(p, "DispatchSystem", new[] { 0, 0, 0 }, "You have been removed from your officer role by a dispatcher");
                        }
                    });
                }
                catch (Exception e)
                {
                    Log.WriteLine(e.ToString());
                }

                DispatchSystem.officers.Remove(ofc);

#if DEBUG
                Log.WriteLine("Removed the officer from the list of officers");
#else
                Log.WriteLineSilent("Removed the officer from the list of officers");
#endif
            }
            else
            {
#if DEBUG
                Log.WriteLine("Officer in list not found, not removing");
#else
                Log.WriteLineSilent("Officer in list not found, not removing");
#endif
            }
        }
        private async Task AddBolo(NetRequestHandler sender, object[] args)
        {
            await Task.FromResult(0);
            if (CheckAndDispose(sender))
                return;
#if DEBUG
            Log.WriteLine("Add bolo Request Recieved");
#else
            Log.WriteLineSilent("Add bolo Request Recieved");
#endif

            string player = (string)args[0];
            string bolo = (string)args[1];

#if DEBUG
            Log.WriteLine($"Adding new Bolo for \"{bolo}\"");
#else
            Log.WriteLineSilent($"Adding new Bolo for \"{bolo}\"");
#endif
            DispatchSystem.ActiveBolos.Add(new Bolo(player, string.Empty, bolo));
        }
        private async Task RemoveBolo(NetRequestHandler sender, object[] args)
        {
            await Task.FromResult(0);
            if (CheckAndDispose(sender))
                return;
#if DEBUG
            Log.WriteLine("Remove bolo Request Recieved");
#else
            Log.WriteLineSilent("Remove bolo Request Recieved");
#endif

            int parse = (int)args[0];

            try
            {
                DispatchSystem.ActiveBolos.RemoveAt(parse);
#if DEBUG
                Log.WriteLine("Removed Active BOLO from the List");
#else
                Log.WriteLineSilent("Removed Active BOLO from the List");
#endif
            }
            catch (ArgumentOutOfRangeException)
            {
#if DEBUG
                Log.WriteLine("Index for BOLO not found, not removing...");
#else
                Log.WriteLineSilent("Index for BOLO not found, not removing...");
#endif
            }
        }
        private async Task AddNote(NetRequestHandler sender, object[] args)
        {
            await Task.FromResult(0);
            if (CheckAndDispose(sender))
                return;
#if DEBUG
            Log.WriteLine("Add Civilian note Request Recieved");
#else
            Log.WriteLineSilent("Add Civilian note Request Recieved");
#endif

            string[] name = new[] { (string)args[0], (string)args[1] };
            string note = (string)args[2];

            Civilian civ = Common.GetCivilianByName(name[0], name[1]);

            if (civ != null)
            {
#if DEBUG
                Log.WriteLine($"Adding the note \"{note}\" to Civilian {civ.First} {civ.Last}");
#else
                Log.WriteLineSilent($"Adding the note \"{note}\" to Civilian {civ.First} {civ.Last}");
#endif
                civ.Notes.Add(note);
            }
            else
#if DEBUG
                Log.WriteLine("Civilian not found, not adding note...");
#else
                Log.WriteLineSilent("Civilian not found, not adding note...");
#endif
        }

        private bool CheckAndDispose(NetRequestHandler sender)
        {
            if (perms.DispatchPermission == Permission.Specific) { if (!perms.DispatchContains(IPAddress.Parse(sender.IP))) { Log.WriteLine($"[{sender.IP}] NOT ENOUGH DISPATCH PERMISSIONS"); return true; } }
            else if (perms.DispatchPermission == Permission.None) { Log.WriteLine($"[{sender.IP}] NOT ENOUGH DISPATCH PERMISSIONS"); return true; }
            return false;
        }
    }
}
