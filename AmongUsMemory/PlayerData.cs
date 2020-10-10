﻿

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HamsterCheese.AmongUsMemory
{
    public class PlayerData
    {
        #region ObserveStates
        private bool observe_dieFlag = false;
        #endregion

        public PlayerControl Instance;
        public System.Action<Vector2, byte> onDie;  

        private string PlayerInfoPTR = null;
        public IntPtr PlayerInfoPTROffset;


        public IntPtr PlayerControllPTR;
        public string PlayerControllPTROffset;

        public IntPtr PlayerPhysicsPTR;
        public string PlayerPhysicsPTROffset;

        Dictionary<string, CancellationTokenSource> Tokens = new Dictionary<string, CancellationTokenSource>();


        [Obsolete] 
        public void ObserveState()
        {
            if (PlayerInfo.HasValue)
            {
                if (observe_dieFlag == false && PlayerInfo.Value.IsDead == 1)
                {
                    observe_dieFlag = true;
                    onDie?.Invoke(Position, PlayerInfo.Value.ColorId);
                }
            }
        }


        /// <summary>
        /// PlayerInfo 가져오기 
        /// </summary>
        public PlayerInfo? PlayerInfo
        {
            get
            {
                if (PlayerInfoPTROffset == IntPtr.Zero)
                {
                    var ptr =  Methods.Call_PlayerControl_GetData(this.PlayerControllPTR);
                    PlayerInfoPTR = ptr.GetAddress();
                    PlayerInfo pInfo = Utils.FromBytes<PlayerInfo>(Cheese.mem.ReadBytes(PlayerInfoPTR, Utils.SizeOf<PlayerInfo>()));
                    PlayerInfoPTROffset = new IntPtr(ptr);
                    m_pInfo = pInfo;
                    return m_pInfo;

                }
                else
                {
                    PlayerInfo pInfo = Utils.FromBytes<PlayerInfo>(Cheese.mem.ReadBytes(PlayerInfoPTR, Utils.SizeOf<PlayerInfo>()));
                    m_pInfo = pInfo;
                    return m_pInfo;
                }

            }
        }
        private PlayerInfo? m_pInfo = null;

        public PlayerPhysics PlayerPhysics
        {
            get
            {
                var mpPtr = Instance.MyPhysics;
                Console.WriteLine("My physics : " + mpPtr.GetAddress());
                var mpBytes = Cheese.mem.ReadBytes(mpPtr.GetAddress(), Utils.SizeOf<PlayerPhysics>());
                var mp = Utils.FromBytes<PlayerPhysics>(mpBytes);
                return mp;
            }
        }
        public LightSource LightSource
        {
            get
            {
                var lsPtr = Instance.myLight;
                Console.WriteLine("light source : " + lsPtr.GetAddress());
                var lsBytes = Cheese.mem.ReadBytes(lsPtr.GetAddress(), Utils.SizeOf<LightSource>());
                var ls = Utils.FromBytes<LightSource>(lsBytes);
                return ls; 
            }
        }
        public void WriteMemory_LightRange(float value)
        {
            var targetPointer = Utils.GetMemberPointer(Instance.myLight, typeof(LightSource), "LightRadius");
            Cheese.mem.WriteMemory(targetPointer.GetAddress(), "float", value.ToString("0.0"));
        }
        public void WriteMemory_ColorID(byte value)
        {
            var targetPointer = Utils.GetMemberPointer(PlayerInfoPTROffset, typeof(PlayerInfo), "ColorId"); 
            Cheese.mem.WriteMemory(targetPointer.GetAddress(), "byte", value.ToString());
        }
        public void WriteMemory_Impostor(byte value)
        {
            var targetPointer = Utils.GetMemberPointer(PlayerInfoPTROffset, typeof(PlayerInfo), "IsImpostor");
            Cheese.mem.WriteMemory(targetPointer.GetAddress(), "byte", value.ToString());
        }

        //I MADE THIS PART
        public void WriteMemory_Speed(float value)
        {
            Console.WriteLine(Instance.MyPhysics);
            Console.WriteLine(PlayerInfoPTROffset);

            var targetPointer = Utils.GetMemberPointer(Instance.MyPhysics, typeof(PlayerPhysics), "Speed");
            Cheese.mem.WriteMemory(targetPointer.GetAddress(), "float", value.ToString());
        }
       
        /// <summary>
        /// Set Player Dead State.
        /// </summary>
        /// <param name="value"></param>
        public void WriteMemory_IsDead(byte value)
        {
            var targetPointer = Utils.GetMemberPointer(PlayerInfoPTROffset, typeof(PlayerInfo), "IsDead");
            Cheese.mem.WriteMemory(targetPointer.GetAddress(), "byte", value.ToString());
        }
        /// <summary>
        /// Set Player KillTimer
        /// </summary>
        /// <param name="value"></param>
        public void WriteMemory_KillTimer(float value)
        {
            var targetPointer = Utils.GetMemberPointer(PlayerControllPTR, typeof(PlayerControl), "killTimer");
            Cheese.mem.WriteMemory(targetPointer.GetAddress(), "float", value.ToString());
        }
        /// <summary>
        /// Set Player KillTimer
        /// </summary>
        /// <param name="value"></param>
        public void WriteMemory_SetNameTextColor(Color value)
        {
            var targetPointer = Utils.GetMemberPointer(Instance.nameText, typeof(TextRenderer), "Color");
            Cheese.mem.WriteMemory(targetPointer.GetAddress(), "float", value.r.ToString("0.0"));
            Cheese.mem.WriteMemory((targetPointer + 4).GetAddress(), "float", value.g.ToString("0.0"));
            Cheese.mem.WriteMemory((targetPointer + 8).GetAddress(), "float", value.b.ToString("0.0"));
            Cheese.mem.WriteMemory((targetPointer + 12).GetAddress(), "float", value.a.ToString("0.0"));
        }
        public void WriteMemory_Speed(int value, IntPtr pointer)
        {
            var targetPointer = Utils.GetMemberPointer(pointer, typeof(PlayerInfo), "Speed");
            Cheese.mem.WriteMemory(targetPointer.GetAddress(), "float", value.ToString());
        }

        public void StopObserveState()
        {
            var key = Tokens.ContainsKey("ObserveState");
            if(key)
            {
                if (Tokens["ObserveState"].IsCancellationRequested == false)
                {
                    Tokens["ObserveState"].Cancel();
                    Tokens.Remove("ObserveState");
                }
            } 
        }
        public void StartObserveState()
        {
            if(Tokens.ContainsKey("ObserveState"))
            {
                Console.WriteLine("Already Observed!");
                return;
            }
            else
            {
                CancellationTokenSource cts = new CancellationTokenSource(); 
                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        if (PlayerInfo.HasValue)
                        {
                            if (observe_dieFlag == false && PlayerInfo.Value.IsDead == 1)
                            {
                                observe_dieFlag = true;
                                onDie?.Invoke(Position, PlayerInfo.Value.ColorId);
                            }
                        }
                        System.Threading.Thread.Sleep(25); 
                    }
                }, cts.Token);

                Tokens.Add("ObserveState", cts);
            }
          
        }

        public Vector2 Position
        {
            get
            {
                if (IsLocalPlayer)
                    return GetMyPosition();
                else
                    return GetSyncPosition();
            }
        }

        public void ReadMemory()
        {
            Instance = Utils.FromBytes<PlayerControl>(Cheese.mem.ReadBytes(PlayerControllPTROffset, Utils.SizeOf<PlayerControl>()));
        }

        public bool IsLocalPlayer
        {
            get
            {
                if (Instance.myLight == IntPtr.Zero) return false;
                else
                {
                    return true;
                }
            }
        }


        public Vector2 GetSyncPosition()
        {
            try
            {
                int _offset_vec2_position = 60;
                int _offset_vec2_sizeOf = 8;
                var netTransform = ((int)Instance.NetTransform + _offset_vec2_position).ToString("X");
                var vec2Data= Cheese.mem.ReadBytes($"{netTransform}",_offset_vec2_sizeOf); // 주소로부터 8바이트 읽는다   
                if (vec2Data != null && vec2Data.Length != 0)
                {
                    var vec2 = Utils.FromBytes<Vector2>(vec2Data);
                    return vec2;
                }
                else
                {
                    return Vector2.Zero;
                }
            }


            catch (Exception e)
            {
                Console.WriteLine(e);
                return Vector2.Zero;
            }
        }
        public Vector2 GetMyPosition()
        {
            try
            {
                int _offset_vec2_position = 80;
                int _offset_vec2_sizeOf = 8;
                var netTransform = ((int)Instance.NetTransform + _offset_vec2_position).ToString("X");
                var vec2Data= Cheese.mem.ReadBytes($"{netTransform}",_offset_vec2_sizeOf); // 주소로부터 8바이트 읽는다  
                if (vec2Data != null && vec2Data.Length != 0)
                {
                    var vec2 = Utils.FromBytes<Vector2>(vec2Data);
                    return vec2;
                }
                else
                {
                    return Vector2.Zero;
                }
            }
            catch
            {
                return Vector2.Zero;
            }
        }


 

    }
}