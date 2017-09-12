/*
 * Information:
 * 
 * 
 * 
 *                __THIS IS A PRE-RELEASE__
 *                -------------------------
 *             There may be some features missing
 *         There may be some bugs in the features here
 * 
 * 
 * 
 * DispatchSystem made by BlockBa5her
 * 
 * Protected under the MIT License
*/

// Definitions
#define ENABLE_VEH // Undefined because work is needed on topic
#undef DEBUG // Leave undefined unless you want unwanted messages in chat
#undef DB // Add future version of DB?

#if !DB
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace DispatchSystem.Server
{
    internal delegate void InvokedCommand(Player player, string[] args);

    public class DispatchSystem : BaseScript
    {
        protected static List<Civilian> civs = new List<Civilian>();
        protected static List<CivilianVeh> civVehs = new List<CivilianVeh>();

        private Dictionary<string, InvokedCommand> commands = new Dictionary<string, InvokedCommand>();

        public DispatchSystem()
        {
            RegisterEvents();
            RegisterCommands();

            Debug.WriteLine("DispatchSystem.Server by BlockBa5her loaded");
            SendMessage("DispatchSystem", new[] { 0, 0, 0 }, "DispatchSystem.Server by BlockBa5her loaded");
        }

        private void RegisterEvents()
        {
            EventHandlers["chatMessage"] += new Action<int, string, string>(OnChatMessage);

            #region Civilian Commands
            EventHandlers["dispatchsystem:setName"] += new Action<string, string, string>(SetName);
            EventHandlers["dispatchsystem:toggleWarrant"] += new Action<string>(ToggleWarrant);
            EventHandlers["dispatchsystem:setCitations"] += new Action<string, int>(SetCitations);
            #endregion

#if ENABLE_VEH
            #region Vehicle Commands
            EventHandlers["dispatchsystem:setVehicle"] += new Action<string, string>(SetVehicle);
            EventHandlers["dispatchsystem:toggleVehStolen"] += new Action<string>(ToggleVehicleStolen);
            EventHandlers["dispatchsystem:toggleVehRegi"] += new Action<string>(ToggleVehicleRegistration);
            EventHandlers["dispatchsystem:toggleVehInsured"] += new Action<string>(ToggleVehicleInsurance);
            #endregion
#endif

            #region Police Commands
            EventHandlers["dispatchsystem:getCivilian"] += new Action<string, string, string>(RequestCivilian);
            EventHandlers["dispatchsystem:addCivNote"] += new Action<string, string, string, string>(AddCivilianNote);
            EventHandlers["dispatchsystem:ticketCiv"] += new Action<string, string, string, string, float>(TicketCivilian);
#if ENABLE_VEH
            EventHandlers["dispatchsystem:getCivilianVeh"] += new Action<string, string>(RequestCivilianVeh);
#endif
            #endregion
        }
        private void RegisterCommands()
        {
            #region Player Commands
            commands.Add("/newname", (p, args) =>
            {
                if (args.Count() < 2)
                {
                    SendUsage(p, "You must have atleast 2 arguments");
                    return;
                }

                TriggerEvent("dispatchsystem:setName", p.Handle, args[0], args[1]);
            });
            commands.Add("/warrant", (p, args) => TriggerEvent("dispatchsystem:toggleWarrant", p.Handle));
            commands.Add("/citations", (p, args) =>
            {
                if (args.Count() < 1)
                {
                    SendUsage(p, "You must have atleast 1 argument");
                    return;
                }

                if (int.TryParse(args[0], out int parse))
                {
                    TriggerEvent("dispatchsystem:setCitations", p.Handle, parse);
                }
                else
                    SendUsage(p, "The argument specified is not a valid number");
            });
            #endregion
#if ENABLE_VEH
            #region Vehicle Commands
            commands.Add("/newveh", (p, args) =>
            {
                if (args.Count() < 1)
                {
                    SendUsage(p, "You must have atleast 2 arguments");
                    return;
                }

                TriggerEvent("dispatchsystem:setVehicle", p.Handle, args[0]);
            });
            commands.Add("/stolen", (p, args) => TriggerEvent("dispatchsystem:toggleVehStolen", p.Handle));
            commands.Add("/registered", (p, args) => TriggerEvent("dispatchsystem:toggleVehRegi", p.Handle));
            commands.Add("/insured", (p, args) => TriggerEvent("dispatchsystem:toggleVehInsured", p.Handle));
            #endregion
#endif
            #region Police Commands
            commands.Add("/2729", (p, args) =>
            {
                if (args.Count() < 2)
                {
                    SendUsage(p, "You must have atleast 2 arguments");
                    return;
                }

                TriggerEvent("dispatchsystem:getCivilian", p.Handle, args[0], args[1]);
            });
#if ENABLE_VEH
            commands.Add("/28", (p, args) =>
            {
                if (args.Count() < 1)
                {
                    SendUsage(p, "You must have atleast 1 argument");
                    return;
                }

                TriggerEvent("dispatchsystem:getCivilianVeh", p.Handle, args[0]);
            });
#endif
            commands.Add("/note", (p, args) =>
            {
                if (args.Count() < 3)
                {
                    SendUsage(p, "You must have atleast 3 arguments");
                    return;
                }

                string note = string.Empty;

                for (int i = 0; i < args.Count(); i++)
                {
                    if (i == 0 || i == 1)
                        continue;

                    note += args[i];
                    note += ' ';
                }

                TriggerEvent("dispatchsystem:addCivNote", p.Handle, args[0], args[1], note);
            });
            commands.Add("/ticket", (p, args) =>
            {
                if (args.Count() < 4)
                {
                    SendUsage(p, "You must have atleast 4 arguments");
                    return;
                }

                string reason = string.Empty;

                for (int i = 0; i < args.Count(); i++)
                {
                    if (i == 0 || i == 1 || i == 2)
                        continue;

                    reason += args[i];
                    reason += ' ';
                }

                if (float.TryParse(args[2], out float amount))
                {
                    TriggerEvent("dispatchsystem:ticketCiv", p.Handle, args[0], args[1], reason, amount);
                }
                else
                    SendUsage(p, "The amount must be a valid number");
            });
            #endregion
        }

        #region Event Methods
        public static void SetName(string handle, string first, string last)
        {
            Player p = GetPlayerByHandle(handle);
            if (p == null) return;

            if (GetCivilian(handle) != null)
            {
                int index = civs.IndexOf(GetCivilian(handle));

                civs[index] = new Civilian(p, first, last, false, 0, new List<string>());

                SendMessage(p, "DispatchSystem", new[] { 0, 0, 0 }, $"New name set to: {civs[index].First} {civs[index].Last}");
            }
            else
            {
                civs.Add(new Civilian(p, first, last, false, 0, new List<string>()));
                int index = civs.IndexOf(GetCivilian(handle));

                SendMessage(p, "DispatchSystem", new[] { 0, 0, 0 }, $"New name set to: {civs[index].First} {civs[index].Last}");
#if DEBUG
                SendMessage(p, "", new[] { 0, 0, 0 }, "Creating new civilian profile...");
#endif
            }
#if ENABLE_VEH
            if (GetCivilianVeh(handle) != null)
            {
                int index = civVehs.IndexOf(GetCivilianVeh(handle));

                civVehs[index] = new CivilianVeh(p);
            }
#endif
        }
        public static void ToggleWarrant(string handle)
        {
            Player p = GetPlayerByHandle(handle);

            if (GetCivilian(handle) != null)
            {
                int index = civs.IndexOf(GetCivilian(handle));
                Civilian last = civs[index];

                civs[index] = new Civilian(p, last.First, last.Last, !last.WarrantStatus, last.CitationCount, last.Notes);

                SendMessage(p, "DispatchSystem", new[] { 0, 0, 0 }, $"Warrant status set to {civs[index].WarrantStatus.ToString()}");
            }
            else
                SendMessage(p, "DispatchSystem", new[] { 0, 0, 0 }, "You must set your name before you can toggle your warrant");
        }
        public static void SetCitations(string handle, int count)
        {
            Player p = GetPlayerByHandle(handle);

            if (GetCivilian(handle) != null)
            {
                int index = civs.IndexOf(GetCivilian(handle));
                Civilian last = civs[index];

                civs[index] = new Civilian(p, last.First, last.Last, last.WarrantStatus, count, last.Notes);

                SendMessage(p, "DispatchSystem", new[] { 0, 0, 0 }, $"Citation count set to {count.ToString()}");
            }
            else
                SendMessage(p, "DispatchSystem", new[] { 0, 0, 0 }, "You must set your name before you can set your citations");
        }
#if ENABLE_VEH
        public static void SetVehicle(string handle, string plate)
        {
            Player p = GetPlayerByHandle(handle);

            if (GetCivilian(handle) == null)
            {
                SendMessage("DispatchSystem", new[] { 0, 0, 0 }, "You must set your name before you can set your vehicle");
                return;
            }

            if (GetCivilianVeh(handle) != null)
            {
                Int32 index = civVehs.IndexOf(GetCivilianVeh(handle));

                civVehs[index] = new CivilianVeh(p, GetCivilian(handle), plate.ToUpper(), false, true, true);

                SendMessage("DispatchSystem", new[] { 0, 0, 0 }, $"New vehicle set to {plate.ToUpper()}");
            }
            else
            {
                civVehs.Add(new CivilianVeh(p, GetCivilian(handle), plate.ToUpper(), false, true, true));

                SendMessage("DispatchSystem", new[] { 0, 0, 0 }, $"New vehicle set to {plate.ToUpper()}");
            }
        }
        public static void ToggleVehicleStolen(string handle)
        {
            Player p = GetPlayerByHandle(handle);

            if (GetCivilian(handle) == null)
            {
                SendMessage("DispatchSystem", new[] { 0, 0, 0 }, "You must set your name before you can set your vehicle stolen");
                return;
            }

            if (GetCivilianVeh(handle) != null)
            {
                int index = civVehs.IndexOf(GetCivilianVeh(handle));
                CivilianVeh last = civVehs[index];

                civVehs[index] = new CivilianVeh(p, GetCivilian(handle), last.Plate, !last.StolenStatus, last.Registered, last.Insured);

                SendMessage(p, "DispatchSystem", new[] { 0, 0, 0 }, $"Stolen status set to {civVehs[index].StolenStatus.ToString()}");
            }
            else
                SendMessage(p, "DispatchSystem", new[] { 0, 0, 0 }, "You must set your vehicle before you can set your vehicle stolen");
        }
        public static void ToggleVehicleRegistration(string handle)
        {
            Player p = GetPlayerByHandle(handle);

            if (GetCivilian(handle) == null)
            {
                SendMessage("DispatchSystem", new[] { 0, 0, 0 }, "You must set your name before you can set your vehicle registration");
                return;
            }

            if (GetCivilianVeh(handle) != null)
            {
                int index = civVehs.IndexOf(GetCivilianVeh(handle));
                CivilianVeh last = civVehs[index];

                civVehs[index] = new CivilianVeh(p, GetCivilian(handle), last.Plate, last.StolenStatus, !last.Registered, last.Insured);

                SendMessage(p, "DispatchSystem", new[] { 0, 0, 0 }, $"Registration status set to {civVehs[index].Registered.ToString()}");
            }
            else
                SendMessage(p, "DispatchSystem", new[] { 0, 0, 0 }, "You must set your vehicle before you can set your Regisration");
        }
        public static void ToggleVehicleInsurance(string handle)
        {
            Player p = GetPlayerByHandle(handle);

            if (GetCivilian(handle) == null)
            {
                SendMessage("DispatchSystem", new[] { 0, 0, 0 }, "You must set your name before you can set your vehicle insurance");
                return;
            }

            if (GetCivilianVeh(handle) != null)
            {
                int index = civVehs.IndexOf(GetCivilianVeh(handle));
                CivilianVeh last = civVehs[index];

                civVehs[index] = new CivilianVeh(p, GetCivilian(handle), last.Plate, last.StolenStatus, last.Registered, !last.Insured);

                SendMessage(p, "DispatchSystem", new[] { 0, 0, 0 }, $"Insurance status set to {civVehs[index].Insured.ToString()}");
            }
            else
                SendMessage(p, "DispatchSystem", new[] { 0, 0, 0 }, "You must set your vehicle before you can set your Insurance");
        }
#endif
        public static void RequestCivilian(string handle, string first, string last)
        {
            Player invoker = GetPlayerByHandle(handle);
            Civilian civ = GetCivilianByName(first, last);

            if (civ != null)
            {
                SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, "Results: ");
                SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, $"First: {civ.First} | Last: {civ.Last}");
                SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, $"Warrant: {civ.WarrantStatus.ToString()}");
                SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, $"Citations: {civ.CitationCount.ToString()}");
                SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, "Notes:");
                if (civ.Notes.Count == 0)
                    SendMessage("", new[] { 0, 0, 0 }, "^9None");
                else
                    civ.Notes.ForEach(x => SendMessage("", new[] { 0, 0, 0 }, $"^7{x}"));
            }
            else
                SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, "That name doesn't exist in the system");
        }
#if ENABLE_VEH
        public static void RequestCivilianVeh(string handle, string plate)
        {
            Player invoker = GetPlayerByHandle(handle);
            CivilianVeh civVeh = GetCivilianVehByPlate(plate);

            if (civVeh != null)
            {
                SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, "Results: ");
                SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, $"Plate: {civVeh.Plate}");
                SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, $"Stolen: {civVeh.StolenStatus.ToString()}");
                SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, $"Registered: {civVeh.Registered.ToString()}");
                SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, $"Insured: {civVeh.Insured.ToString()}");
                if (civVeh.Registered) SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, $"R/O: {civVeh.Owner.First} {civVeh.Owner.Last}");
            }
            else
                SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, "That name doesn't exist in the system");
        }
#endif
        public static void AddCivilianNote(string invokerHandle, string first, string last, string note)
        {
            Player invoker = GetPlayerByHandle(invokerHandle);
            Civilian civ = GetCivilianByName(first, last);

            if (civ != null)
            {
                int index = civs.IndexOf(civ);
                civs[index].Notes.Add(note);
                SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, $"Note of \"{note}\" has been added to the Civilian");
            }
            else
                SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, "That name doesn't exist in the system");
        }
        public static void TicketCivilian(string invokerHandle, string first, string last, string reason, float amount)
        {
            Player invoker = GetPlayerByHandle(invokerHandle);
            Civilian civ = GetCivilianByName(first, last);

            if (civ != null)
            {
                int index = civs.IndexOf(civ);
                Player p = civs[index].Source;
                Civilian _last = civs[index];
                civs[index] = new Civilian(_last.Source, _last.First, _last.Last, _last.WarrantStatus, _last.CitationCount + 1, _last.Notes);
                SendMessage(p, "Ticket", new[] { 255, 0, 0 }, $"{invoker.Name} tickets you for ${amount.ToString()} because of {reason}");
                SendMessage(p, "DispatchSystem", new[] { 0, 0, 0 }, $"You successfully ticketed {p.Name} for ${amount.ToString()}");
            }
            else
                SendMessage(invoker, "DispatchSystem", new[] { 0, 0, 0 }, "That name doesn't exist in the system");
        }
#endregion

        private void OnChatMessage(int source, string n, string msg)
        {
            Player p = this.Players[source];
            var args = msg.Split(' ').ToList();
            var cmd = args[0];
            args.RemoveAt(0);

            if (commands.ContainsKey(cmd.ToLower()))
            {
                CancelEvent();
                commands[cmd].Invoke(p, args.ToArray());
            }
        }

#region Common
        private static Civilian GetCivilian(string pHandle)
        {
            foreach (var item in civs)
            {
                if (item.Source.Handle == pHandle)
                    return item;
            }
            
            return null;
        }
        private static Civilian GetCivilianByName(string first, string last)
        {
            foreach (var item in civs)
            {
                if (item.First.ToLower() == first.ToLower() && item.Last.ToLower() == last.ToLower())
                    return item;
            }

            return null;
        }
#if ENABLE_VEH
        private static CivilianVeh GetCivilianVeh(string pHandle)
        {
            foreach (var item in civVehs)
            {
                if (item.Source.Handle == pHandle)
                    return item;
            }

            return null;
        }
        private static CivilianVeh GetCivilianVehByPlate(string plate)
        {
            foreach (var item in civVehs)
            {
                if (item.Plate.ToLower() == plate.ToLower())
                    return item;
            }

            return null;
        }

        static string lastPlate = null;
        public static void TransferLicense(string license) => lastPlate = license;
        public static string GetLicensePlate(Player p)
        {
            TriggerClientEvent(p, "dispatchsystem:requestLP");

            while (lastPlate == null)
                Delay(10).Wait();

            string @return = null;
            if (lastPlate != "---------")
                @return = lastPlate;

            lastPlate = null;

            return @return;
        }
#endif

        private static Player GetPlayerByHandle(string handle)
        {
            foreach (var plr in new PlayerList())
                if (plr.Handle == handle) return plr;

            return null;
        }


        #region Chat Commands
        private static void WriteChatLine(Player p) => TriggerClientEvent(p, "chatMessage", "", new[] { 0, 0, 0 }, "\n");
        private static void WriteChatLine() => TriggerClientEvent("chatMessage", "", new[] { 0, 0, 0 }, "\n");
        private static void SendMessage(Player p, string title, int[] rgb, string msg) => TriggerClientEvent(p, "chatMessage", title, rgb, msg);
        private static void SendMessage(string title, int[] rgb, string msg) => TriggerClientEvent("chatMessage", title, rgb, msg);
        private static void SendUsage(Player p, string usage) => TriggerClientEvent(p, "chatMessage", "Usage", new[] { 255, 255, 255 }, usage);
#endregion

        private static void CancelEvent() => Function.Call(Hash.CANCEL_EVENT);
#endregion
    }
}

#endif