using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SoulsFormats;
using SoulsIds;
using static SoulsIds.GameSpec;

namespace StatusMod
{
    public class Program
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        [DllImport("kernel32")]
        static extern bool AllocConsole();

        [DllImport("kernel32")]
        static extern bool FreeConsole();

        private static string name = "floor";
        private static FromGame gameType = FromGame.SDT;

        public static int Main(string[] args)
        {
            if (true) {
                name = "rot";
                gameType = FromGame.ER;
            }
            string upper = name[0].ToString().ToUpper() + name.Substring(1);

            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            AllocConsole();
            bool success = true;
            try
            {
                RunCurrentDirectory();
                // GenerateVariants();
            }
            catch (Exception e)
            {
                success = false;
                Console.WriteLine(e);
            }
            Console.WriteLine();
            if (success)
            {
                Console.WriteLine($"{upper}Mod succeeded! You may close this window now.");
            }
            else
            {
                Console.WriteLine($"{upper}Mod failed!");
            }
            Console.ReadKey();
            FreeConsole();
            return success ? 0 : 1;
        }

        public static void RunCurrentDirectory()
        {
            RunMod(".", ".", ".");
        }

        public static void GenerateVariants()
        {
            foreach (string variant in Directory.GetDirectories(@"C:\Program Files (x86)\Steam\steamapps\common\Sekiro\floor"))
            {
                if (variant.EndsWith("custom") || !File.Exists(Path.Combine(variant, "floorconfig.ini")))
                {
                    continue;
                }
                RunMod(variant, @"C:\Program Files (x86)\Steam\steamapps\common\Sekiro", variant);
            }
        }

        private static string GetVar(string ini, string section, string var)
        {
            StringBuilder val = new StringBuilder(255);
            GetPrivateProfileString(section, var, "", val, 255, ini);
            string result = val.ToString();
            if (string.IsNullOrWhiteSpace(result))
            {
                Console.WriteLine($"{name}config.ini variable {var} in section [{section}] not found or missing");
            }
            return result;
        }

        enum Effect { Poison, Fire, Terror, Enfeeble, Rot }

        enum Start { None, Healthy, Delayed }

        private static void RunMod(string configDir, string inDir, string outDir)
        {
            string ini = new FileInfo(Path.Combine(configDir, $"{name}config.ini")).FullName.ToString();
            if (!File.Exists(ini))
            {
                throw new Exception($"{ini} not found");
            }

            if (!File.Exists("oo2core_6_win64.dll"))
            {
                string gameDir = gameType == FromGame.ER ? @"ELDEN RING\Game" : "Sekiro";
                string[] searchOrder = { @"..\oo2core_6_win64.dll", @"..\..\oo2core_6_win64.dll", $@"C:\Program Files (x86)\Steam\steamapps\common\{gameDir}\oo2core_6_win64.dll" };
                foreach (string search in searchOrder)
                {
                    if (File.Exists(search))
                    {
                        File.Copy(search, "oo2core_6_win64.dll");
                        break;
                    }
                }
                if (!File.Exists("oo2core_6_win64.dll"))
                {
                    throw new Exception($"Oodle not found. Copy oo2core_6_win64.dll from {gameDir} directory");
                }
            }

            bool isSekiro = gameType == FromGame.SDT;
            bool isElden = gameType == FromGame.ER;

            GameEditor game = new GameEditor(gameType);
            game.Spec.GameDir = inDir;
            game.Spec.DefDir = isSekiro ? "SdtDefs" : "ErDefs";
            Dictionary<string, PARAMDEF> defs = game.LoadDefs();
            // Dictionary<string, PARAM> param = game.LoadParams(defs);
            ParamDictionary param = new ParamDictionary { Defs = defs, Inner = game.LoadParams(new Dictionary<string, PARAMDEF>()) };
            // Checked on access
            if (false && !param.ContainsKey("SpEffectParam") || !param.ContainsKey("HitMtrlParam"))
            {
                throw new Exception($"Missing required params: only found [{string.Join(", ", param.Keys)}] from defs [{string.Join(", ", defs.Keys)}]");            }

            string typeStr = GetVar(ini, "mod", "statusEffect").ToLowerInvariant();
            if (!Enum.TryParse(typeStr, true, out Effect type))
            {
                throw new Exception($"Unsupported statusEffect {typeStr}");
            }
            if (isElden != (type == Effect.Rot))
            {
                throw new Exception($"Unsupported statusEffect {typeStr}");
            }
            Start start = Start.None;
            if (isSekiro)
            {
                string startStr = GetVar(ini, "mod", "startMod").ToLowerInvariant();
                if (!Enum.TryParse(startStr, true, out start))
                {
                    throw new Exception($"Unsupported startMod {startStr}");
                }
            }

            bool applyEverywhere = isElden || GetVar(ini, "mod", "applyEverywhere") == "1";
            Console.WriteLine($"Creating {typeStr} effect");

            PARAM.Row copySpEffect(int source, int target, string desc)
            {
                Console.WriteLine($"- Copying {desc} effect from {source} to {target}");
                PARAM.Row ret = param["SpEffectParam"][target];
                if (ret == null)
                {
                    ret = new PARAM.Row(target, "", param["SpEffectParam"].AppliedParamdef);
                    param["SpEffectParam"].Rows.Add(ret);
                }
                GameEditor.CopyRow(param["SpEffectParam"][source], ret);
                return ret;
            }

            int neutral = 5020;

            // 4066: dummy speffect to copy to 4067 for player in floor mode
            if (!isElden)
            {
                PARAM.Row dummySp = copySpEffect(neutral, 4066, "trigger");
                dummySp["effectEndurance"].Value = 0f;
            }

            // 4067: rate-limiting speffect
            PARAM.Row rateSp = copySpEffect(isElden ? 4050 : 4002, 4067, "rate");
            string freqStr = GetVar(ini, typeStr, $"{typeStr}TickFrequency");
            if (!float.TryParse(freqStr, out float freq))
            {
                throw new Exception($"{typeStr}TickFrequency of {freqStr} is not a decimal number");
            }
            string cycle = isElden ? "cycleOccurrenceSpEffectId" : "cycleOccurenceSpEffectId";
            param["SpEffectParam"][4067]["motionInterval"].Value = freq;
            param["SpEffectParam"][4067][cycle].Value = 4068;

            if (isElden)
            {
                // In Elden Ring, buildup speffect and replace speffect are the same
                PARAM.Row buildupSp = copySpEffect(4051, 4068, "final");
                string amountStr = GetVar(ini, typeStr, $"{typeStr}TickAmount");
                if (!int.TryParse(amountStr, out int amount))
                {
                    throw new Exception($"{typeStr}TickAmount of {freqStr} is not an integer");
                }
                buildupSp["diseaseAttackPower"].Value = amount;

                string hpRateStr = GetVar(ini, typeStr, $"{typeStr}HpPercent");
                if (!float.TryParse(hpRateStr, out float hpRate))
                {
                    throw new Exception($"{typeStr}HpPercent of {hpRateStr} is not a decimal number");
                }
                string hpPointStr = GetVar(ini, typeStr, $"{typeStr}HpRaw");
                if (!int.TryParse(hpPointStr, out int hpPoint))
                {
                    throw new Exception($"{typeStr}HpRaw of {hpPointStr} is not an integer");
                }
                buildupSp["changeHpRate"].Value = hpRate;
                buildupSp["changeHpPoint"].Value = hpPoint;

                string durationStr = GetVar(ini, typeStr, $"{typeStr}EffectDuration");
                if (!float.TryParse(durationStr, out float duration))
                {
                    throw new Exception($"{typeStr}EffectDuration of {freqStr} is not a decimal number");
                }
                buildupSp["effectEndurance"].Value = duration;

                // Installation on 100620 (right) 100621 (left). Some other mods use this, so they won't be compatible.
                param["SpEffectParam"][100620][cycle].Value = 4067;
                param["SpEffectParam"][100621][cycle].Value = 4067;

                DCX.Type overrideDcx = DCX.Type.DCX_DFLT_11000_44_9;
                if (outDir == ".")
                {
                    SFUtil.Backup($@"{outDir}\regulation.bin");
                }
                string path = new FileInfo($@"{outDir}\regulation.bin").FullName;
                Console.WriteLine($"Writing parameters to {path}");
                game.OverrideBndRel($@"{inDir}\regulation.bin", path, param.Inner, f => f.AppliedParamdef == null ? null : f.Write(), dcx: overrideDcx);
            }
            else
            {
                // 4068: buildup speffect
                Dictionary<Effect, int> buildupSpBase = new Dictionary<Effect, int>
                {
                    [Effect.Poison] = 4003,
                    [Effect.Fire] = 4011,
                    [Effect.Terror] = 9200,
                    [Effect.Enfeeble] = 9600,
                };
                PARAM.Row buildupSp = copySpEffect(buildupSpBase[type], 4068, "buildup");
                if (type == Effect.Fire)
                {
                    if (GetVar(ini, "fire", "removeFireShake") == "1")
                    {
                        buildupSp["EzStateBehaviorRequestId"].Value = 0;
                    }
                    if (GetVar(ini, "fire", "removeFireBuildupDamage") == "1")
                    {
                        buildupSp["changeHpRate"].Value = 0f;
                        buildupSp["changeHpPoint"].Value = 0;
                    }
                }
                Dictionary<Effect, string> registFields = new Dictionary<Effect, string>
                {
                    [Effect.Poison] = "registPoison",
                    [Effect.Fire] = "registBlood",
                    [Effect.Terror] = "registToxic",
                    [Effect.Enfeeble] = "registCurse",
                };
                string registField = registFields[type];
                string amountStr = GetVar(ini, typeStr, $"{typeStr}TickAmount");
                if (!int.TryParse(amountStr, out int amount))
                {
                    throw new Exception($"{typeStr}TickAmount of {freqStr} is not an integer");
                }
                // The regist is in the buildup sp for most effects but in tick sp for fire
                if (type != Effect.Fire)
                {
                    buildupSp[registField].Value = amount;
                }

                // 4069: effect ticking speffect, if applicable
                int tickSpBase = (int)buildupSp["replaceSpEffectId"].Value;
                if (tickSpBase > 0)
                {
                    PARAM.Row tickSp = copySpEffect(tickSpBase, 4069, "final");
                    buildupSp["replaceSpEffectId"].Value = 4069;
                    if (type == Effect.Poison || type == Effect.Fire)
                    {
                        string multStr = GetVar(ini, typeStr, $"{typeStr}DamageMultiplier");
                        if (!float.TryParse(multStr, out float mult))
                        {
                            throw new Exception($"{typeStr}DamageMultiplier of {multStr} is not a decimal number");
                        }
                        if (Math.Abs(mult - 1) > 0.00001)
                        {
                            // By default, poison: (0.25, 12), fire: (0.5, 4)
                            // Don't use existing field values in order to make repeated application possible
                            tickSp["changeHpRate"].Value = (type == Effect.Poison ? 0.25 : 0.5) * mult;
                            tickSp["changeHpPoint"].Value = (int)Math.Ceiling((type == Effect.Poison ? 12 : 4) * mult);
                        }
                    }
                    if (type == Effect.Fire)
                    {
                        tickSp[registField].Value = amount;
                    }
                    if (type != Effect.Terror)
                    {
                        string durationStr = GetVar(ini, typeStr, $"{typeStr}EffectDuration");
                        if (!float.TryParse(durationStr, out float duration))
                        {
                            throw new Exception($"{typeStr}EffectDuration of {freqStr} is not a decimal number");
                        }
                        tickSp["effectEndurance"].Value = duration;
                    }
                }
                param["SpEffectParam"].Rows.Sort((a, b) => a.ID.CompareTo(b.ID));

                // Adjust curing items. Exclude Divine Grass I guess, since it's finite and single-use
                // This is particularly important for terror. The longest bosses take 2-3 minutes when executed perfectly,
                // but cheeseless Gyoubu requires around 45 second tick time, using a single resurrection. So with 10 terror
                // full heals, cut this efficacy in half, to allow for 7.5 minutes -> 3.75 minutes.
                Dictionary<Effect, int[]> cureSpIds = new Dictionary<Effect, int[]>
                {
                    [Effect.Poison] = new int[] { 3201, 3301 },
                    [Effect.Fire] = new int[] { 3211, 3311 },
                    [Effect.Terror] = new int[] { 3221, 3321 },
                };
                int defaultCure = 99999;
                float terrorResistTime = 30;
                if (type == Effect.Terror)
                {
                    string resistStr = GetVar(ini, typeStr, $"{typeStr}ResistanceDuration");
                    if (!float.TryParse(resistStr, out terrorResistTime))
                    {
                        throw new Exception($"{typeStr}ResistanceDuration of {resistStr} is not a decimal number");
                    }
                }
                foreach (KeyValuePair<Effect, int[]> cureSpId in cureSpIds)
                {
                    Effect cureType = cureSpId.Key;
                    int cure = defaultCure;
                    if (type == cureType)
                    {
                        string cureStr = GetVar(ini, typeStr, $"{typeStr}CureAmount");
                        if (!int.TryParse(cureStr, out cure))
                        {
                            throw new Exception($"{typeStr}CureAmount of {cureStr} is not an integer");
                        }
                    }
                    foreach (int sp in cureSpId.Value)
                    {
                        param["SpEffectParam"][sp][registFields[cureType]].Value = -cure;
                        if (cureType == Effect.Terror)
                        {
                            param["SpEffectParam"][sp + 1]["effectEndurance"].Value = terrorResistTime;
                        }
                    }
                }

                // Floors
                Console.WriteLine($"Applying {typeStr} effect {(applyEverywhere ? "everywhere" : "to floors")}");
                foreach (PARAM.Row row in param["HitMtrlParam"].Rows)
                {
                    if (row.ID == 0)
                    {
                        row["spEffectId0"].Value = applyEverywhere ? 4066 : -1;
                        continue;
                    }
                    int[] hit = new int[] { (int)row["spEffectId0"].Value, (int)row["spEffectId1"].Value };
                    if (hit.Contains(4066) || hit.Contains(4000) || hit.Contains(4002))
                    {
                        continue;
                    }
                    if (hit[0] == -1)
                    {
                        row["spEffectId0"].Value = 4066;
                    }
                    else if (hit[1] == -1)
                    {
                        row["spEffectId1"].Value = 4066;
                    }
                    else
                    {
                        throw new Exception($"Unable to apply effect to floor material {row.ID} since both slots are used ({hit[0]}, {hit[1]})");
                    }

                    // Finally dummy->actual transfer, for player
                    EMEVD emevd = EMEVD.Read($@"{inDir}\event\common.emevd.dcx");
                    int dummyEvent = 11215185;
                    int healthyEvent = 11215186;
                    int[] events = { dummyEvent, healthyEvent };
                    EMEVD.Event constr = emevd.Events.Find(e => e.ID == 0);
                    if (constr == null)
                    {
                        throw new Exception("commond.emevd.dcx missing constructor???");
                    }
                    constr.Instructions.RemoveAll(instr =>
                    {
                        if (instr.ID == 2000 && instr.Bank == 0)
                        {
                            List<object> args = instr.UnpackArgs(Enumerable.Repeat(EMEVD.Instruction.ArgType.Int32, instr.ArgData.Length / 4));
                            if (args.Count >= 2 && events.Contains((int)args[1]))
                            {
                                return true;
                            }
                        }
                        return false;
                    });
                    emevd.Events.RemoveAll(e => events.Contains((int)e.ID));

                    constr.Instructions.Add(new EMEVD.Instruction(2000, 0, new List<object> { 0, dummyEvent, 0 }));
                    EMEVD.Event newEvent = new EMEVD.Event(dummyEvent);
                    newEvent.Instructions.AddRange(new List<EMEVD.Instruction>
                    {
                        new EMEVD.Instruction(4, 5, new List<object> { (sbyte)-1, 10000, 4066, (byte)1, (sbyte)0, 1f }),
                        new EMEVD.Instruction(4, 5, new List<object> { (sbyte)-1, 10000, 4000, (byte)1, (sbyte)0, 1f }),
                        new EMEVD.Instruction(4, 5, new List<object> { (sbyte)-1, 10000, 4002, (byte)1, (sbyte)0, 1f }),
                        new EMEVD.Instruction(0, 0, new List<object> { (sbyte)1, (byte)1, (sbyte)-1 }),
                        new EMEVD.Instruction(0, 0, new List<object> { (sbyte)0, (byte)1, (sbyte)1 }),
                    });
                    if (start == Start.Delayed)
                    {
                        newEvent.Instructions.Add(new EMEVD.Instruction(1003, 2, new List<object> { (byte)1, (byte)0, (byte)0, 6300 }));
                    }
                    newEvent.Instructions.AddRange(new List<EMEVD.Instruction>
                {
                    new EMEVD.Instruction(2004, 8, new List<object> { 10000, 4067 }),
                    new EMEVD.Instruction(1000, 4, new List<object> { (byte)1 }),
                });
                    emevd.Events.Add(newEvent);

                    // Partial health start. Can't figure out how this works so just counteract it.
                    if (start == Start.Healthy)
                    {
                        constr.Instructions.Add(new EMEVD.Instruction(2000, 0, new List<object> { 0, healthyEvent, 0 }));
                        newEvent = new EMEVD.Event(healthyEvent);
                        newEvent.Instructions = new List<EMEVD.Instruction>
                        {
                            new EMEVD.Instruction(1003, 2, new List<object> { (byte)0, (byte)1, (byte)0, 6300 }),
                            new EMEVD.Instruction(1003, 201, new List<object> { (byte)0, (byte)0, 10000, 1122235, 1 }),
                            new EMEVD.Instruction(1001, 1, new List<object> { 1 }),
                            new EMEVD.Instruction(2004, 8, new List<object> { 10000, 8000 }),
                        };
                        emevd.Events.Add(newEvent);
                    }

                    bool dryrun = false;
                    if (outDir == ".")
                    {
                        SFUtil.Backup($@"{outDir}\param\gameparam\gameparam.parambnd.dcx");
                        SFUtil.Backup($@"{outDir}\event\common.emevd.dcx");
                    }
                    string path = new FileInfo($@"{outDir}\param\gameparam\gameparam.parambnd.dcx").FullName;
                    Console.WriteLine($"Writing parameters to {path}");
                    if (!dryrun) game.OverrideBnd($@"{inDir}\param\gameparam\gameparam.parambnd.dcx", Path.GetDirectoryName(path), param.Inner, f => f.AppliedParamdef == null ? null : f.Write());
                    path = new FileInfo($@"{outDir}\event\common.emevd.dcx").FullName;
                    Console.WriteLine($"Writing event script to {path}");
                    if (!dryrun) emevd.Write(path);
                }
            }
        }
    }
}
