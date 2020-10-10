
using HamsterCheese.AmongUsMemory;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace YourCheese
{
    class Globals
    {
        public static byte clientImposter = 0;
        public static byte clientDead = 0;
        public static string[] imposters = new string[] {};
        public static int killdelay = 0;
        public static float lightrange = 5;
        public static float speed = 2;
    }
    class Program
    {
        static int tableWidth = 75;

        static List<PlayerData> playerDatas = new List<PlayerData>();
        static List<PlayerPhysics> playerPhysics = new List<PlayerPhysics>();
        static bool UpdateCheat()
        {
            while (true)
            { 
                Console.Clear();
                Console.WriteLine("Test Read Player Datas..");
                PrintRow("offset", "Name", "OwnerId", "PlayerId", "spawnid", "spawnflag");
                PrintLine();

                if (Globals.imposters.Length > 0)
                {
                    Array.Resize(ref Globals.imposters, 0);
                }

                foreach (var data in playerDatas)
                {
                    if (data.IsLocalPlayer)
                    {
                        
                        Console.ForegroundColor = ConsoleColor.Green;
                        //set your player name text renderer color
                        data.WriteMemory_SetNameTextColor(new Color(0,1,0,1)); 
                    }

                    if (data.PlayerInfo.Value.IsDead == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    var Name = HamsterCheese.AmongUsMemory.Utils.ReadString(data.PlayerInfo.Value.PlayerName);

                    if (data.PlayerInfo.Value.IsImpostor == 1)
                    {
                        Array.Resize(ref Globals.imposters, Globals.imposters.Length + 1);
                        Globals.imposters[Globals.imposters.Length-1] = Name;
                    }

                    if (data.IsLocalPlayer == true)
                    {
                        ///data.WriteMemory_Impostor(Globals.clientImposter);
                        ///data.WriteMemory_KillTimer(Globals.killdelay);
                        ///data.WriteMemory_IsDead(Globals.clientDead);

                        data.WriteMemory_LightRange(Globals.lightrange);
                        data.WriteMemory_Speed(Globals.speed);
                    }
                    Console.WriteLine();
                    PrintRow($"{(data.IsLocalPlayer == true ? "Me->" : "")}{data.PlayerControllPTROffset}", $"{Name}", $"{data.Instance.OwnerId}", $"{data.Instance.PlayerId}", $"{data.Instance.SpawnId}", $"{data.Instance.SpawnFlags}");
                    Console.ForegroundColor = ConsoleColor.White;

                    PrintLine();
                }
                Console.WriteLine("The imposter is: ");
                Console.WriteLine("[{0}]", string.Join(", ", Globals.imposters));
                System.Threading.Thread.Sleep(1000);
                
            }
        }
        static void Main(string[] args)
        {

            // Cheat Init
            if (HamsterCheese.AmongUsMemory.Cheese.Init())
            { 
                // Update Player Data When Every Game
                HamsterCheese.AmongUsMemory.Cheese.ObserveShipStatus((x) =>
                {
                    
                    //stop observe state for init. 
                    foreach(var player in playerDatas) 
                        player.StopObserveState(); 


                    playerDatas = HamsterCheese.AmongUsMemory.Cheese.GetAllPlayers();
                    
                  
                 
                    foreach (var player in playerDatas)
                    {
                        player.onDie += (pos, colorId) => {
                            Console.WriteLine("OnPlayerDied! Color ID :" + colorId);
                        }; 
                        // player state check
                        player.StartObserveState();
                    }

                
                });

                // Cheat Logic
                CancellationTokenSource cts = new CancellationTokenSource();
                Task.Factory.StartNew(
                    UpdateCheat
                , cts.Token); 
            }

            System.Threading.Thread.Sleep(1000000);
        }

        static void PrintLine()
        {
            Console.WriteLine(new string('-', tableWidth));
        }

        static void PrintRow(params string[] columns)
        {
            int width = (tableWidth - columns.Length) / columns.Length;
            string row = "|";

            foreach (string column in columns)
            {
                row += AlignCentre(column, width) + "|";
            }

            Console.WriteLine(row);

            
        }

        static string AlignCentre(string text, int width)
        {
            text = text.Length > width ? text.Substring(0, width - 3) + "..." : text;

            if (string.IsNullOrEmpty(text))
            {
                return new string(' ', width);
            }
            else
            {
                return text.PadRight(width - (width - text.Length) / 2).PadLeft(width);
            }
        } 
    }
}


