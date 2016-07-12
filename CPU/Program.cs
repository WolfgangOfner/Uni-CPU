using System;
using System.Collections;
using System.IO;

namespace CPU
{
    class Program
    {
        static int[] register = new int[9]; // A - H, R
        static int flagC = 0;
        static int flagN = 0;
        static int flagZ = 0;
        static string[] rom = new string[256];
        static int[] ram = new int[256];
        static Stack stack = new Stack(256);
        static int sp = 0;
        static int bp = 0;
        static int pc = 0;
        static string ir = string.Empty;
        static int mar = 0;                 // memory address register
        static string mdr = string.Empty;   // memory data register       
        static bool traceFetch = false;
        static bool traceDecode = false;

        static void Main(string[] args)
        {
            string line;
            int counter = 0;
            // Read the file with the program
            StreamReader file = new StreamReader("c:\\ggt.txt");

            while ((line = file.ReadLine()) != null && counter < 256)
            {
                rom[counter] = line;
                counter++;
            }

            file.Close();

            while (true)
            {
                Fetch();
                Decode();
            }
        }

        private static void Fetch()
        {
            mar = pc;
            mdr = rom[mar];
            ir = mdr;
            pc++;

            if (traceFetch)
            {
                TraceFetch();
            }
        }

        private static void Decode()
        {
            string[] opCode = ir.Split(' ');

            if (traceDecode)
            {
                SetBasePointer();
                stack.Push(opCode[0]);
                sp++;
                TraceDecode();
            }

            switch (opCode[0].ToUpper())
            {
                case "RDUMP":
                    ExecuteDumpRegisters();
                    break;
                case "TRACE_FETCH":
                    traceFetch = !traceFetch;
                    break;
                case "TRACE_DECODE":
                    traceDecode = !traceDecode;
                    break;
                case "MDUMP":
                    ExecuteMdump();
                    break;
                case "SDUMP":
                    ExecuteSdump();
                    break;

                case "MOV":
                    // opCode[0] == instruction
                    // opCode[1] == destination
                    // opCode[2] == source
                    if (opCode.Length != 3 || (opCode[1].Contains("@") && opCode[1].Contains("@")))
                    {
                        Error();
                    }

                    // source
                    // direct
                    if (opCode[2].Substring(0, 1).Equals("#"))
                    {
                        try
                        {
                            SetBasePointer();
                            stack.Push(opCode[2].Substring(1));
                            sp++;
                        }
                        catch (Exception)
                        {
                            Error();
                        }
                    }
                    // from ram
                    else if (opCode[2].Substring(0, 1).Equals("@"))
                    {
                        try
                        {
                            mar = Convert.ToInt32(opCode[2].Substring(1));
                            mdr = ram[mar].ToString();
                        }
                        catch (Exception)
                        {
                            Error();
                        }

                    }
                    else if (opCode[2].Substring(0, 1).Equals("$"))
                    {
                        Object[] myStandardArray = stack.ToArray();
                        stack.Push(myStandardArray[sp - (Convert.ToInt32(opCode[2].Substring(1)))]);
                        sp++;
                    }
                    // from register
                    else
                    {
                        SetBasePointer();
                        stack.Push(opCode[2].Substring(0, 1));
                        sp++;
                        GetRegisterValue();
                    }

                    // destination
                    // into ram
                    if (opCode[1].Substring(0, 1).Equals("@"))
                    {
                        try
                        {
                            mar = Convert.ToInt32(opCode[1].Substring(1));
                            mdr = stack.Pop().ToString();
                            sp--;
                            ram[mar] = Convert.ToInt32(mdr);
                        }
                        catch (Exception)
                        {
                            Error();
                        }
                    }
                    else
                    {
                        if (opCode[1].Length > 1)
                        {
                            Error();
                        }                        
                        else
                        {
                            SetBasePointer();
                            stack.Push(opCode[1]);
                            sp++;
                            GetRegisterIndex();
                                                   
                            if (opCode[2].Contains("#"))
                            {
                                try
                                {
                                    register[(int)stack.Pop()] = Convert.ToInt32(opCode[2].Substring(1));
                                    stack.Pop();
                                    sp--;
                                    sp--;
                                }
                                catch (Exception)
                                {
                                    Error();
                                }
                            }
                            else
                            {
                                register[(int)stack.Pop()] = (int)stack.Pop();
                                sp--;
                                sp--;
                            }
                        }
                    }

                    break;

                case "MOVI":
                    // opCode[0] == instruction
                    // opCode[1] == [destination]
                    // opCode[2] == source
                    if (opCode.Length != 3)
                    {
                        Error();
                    }

                    // check if its movi [destination], source
                    if (opCode[1].Contains("["))
                    {
                        // source
                        // direct
                        if (opCode[2].Substring(0, 1).Equals("#"))
                        {
                            try
                            {
                                mdr = opCode[2].Substring(1);
                            }
                            catch (Exception)
                            {
                                Error();
                            }
                        }
                        // register
                        else if (opCode[2].Length == 1)
                        {
                            SetBasePointer();
                            stack.Push(opCode[2].Substring(0, 1));
                            sp++;
                            GetRegisterValue();
                            mdr = stack.Pop().ToString();
                            sp--;
                        }
                        else
                        {
                            Error();
                        }

                        // destination register indirect
                        if (opCode[1].Substring(0, 1).Equals("[") && opCode[1].Substring(opCode[1].Length - 1).Equals("]") && opCode[1].Length == 3)
                        {
                            SetBasePointer();
                            stack.Push(opCode[1].Substring(1, 1));
                            sp++;
                            GetRegisterValue();
                            mar = (int)stack.Pop();
                            sp--;

                            try
                            {
                                ram[mar] = Convert.ToInt32(mdr);
                            }
                            catch (Exception)
                            {
                                Error();
                            }
                        }
                        else
                        {
                            Error();
                        }
                    }
                    // movi destination, [source]
                    else
                    {
                        SetBasePointer();
                        stack.Push(opCode[1]);
                        GetRegisterIndex();

                        // source
                        if (opCode[2].Substring(0, 1).Equals("[") && opCode[2].Substring(opCode[1].Length - 1).Equals("]") && opCode[2].Length == 3)
                        {
                            SetBasePointer();
                            stack.Push(opCode[2].Substring(1, 1));
                            sp++;
                            GetRegisterValue();
                            mar = (int)stack.Pop();
                            sp--;

                            mdr = ram[mar].ToString();

                            // copy
                            try
                            {
                                register[(int)stack.Pop()] = Convert.ToInt32(mdr);
                                sp--;
                            }
                            catch (Exception)
                            {
                                Error();
                            }
                        }
                        else
                        {
                            Error();
                        }
                    }

                    break;

                case "HALT":
                    SetBasePointer();
                    stack.Push(0);
                    sp++;

                    ExecuteHalt();
                    break;

                case "PUSH":
                    SetBasePointer();
                    stack.Push(opCode[1]);
                    sp++;

                    ExecutePUSH();
                    break;

                case "POP":
                    SetBasePointer();
                    stack.Push(opCode[1]);
                    sp++;
                    GetRegisterIndex();

                    ExecutePOP();
                    break;

                case "CALL":
                    if (opCode[1].Substring(0, 1).Equals("@"))
                    {
                        SetBasePointer();
                        stack.Push(pc);
                        sp++;

                        try
                        {
                            pc = Convert.ToInt32(opCode[1].Substring(1));
                        }
                        catch (Exception)
                        {
                            Error();
                        }
                    }
                    else
                    {
                        Error();
                    }
                    break;

                case "RET":
                    ExecuteRET();
                    break;

                case "JMP":
                    SetBasePointer();
                    stack.Push(opCode[1]);
                    sp++;

                    ExecuteJMP();
                    break;

                case "JR":
                    SetBasePointer();
                    stack.Push(opCode[1]);
                    sp++;

                    ExecuteJR();
                    break;

                case "JRC":
                    SetBasePointer();
                    stack.Push(opCode[1]);
                    sp++;

                    ExecuteJRC();
                    break;

                case "JRZ":
                    SetBasePointer();
                    stack.Push(opCode[1]);
                    sp++;

                    ExecuteJRZ();
                    break;

                case "JRN":
                    SetBasePointer();
                    stack.Push(opCode[1]);
                    sp++;

                    ExecuteJRN();
                    break;

                case "AND":
                    SetBasePointer();
                    stack.Push(opCode[1]);
                    sp++;
                    GetRegisterIndex();
                    
                    stack.Push(opCode[2]);
                    sp++;
                    GetRegisterIndex();

                    ExecuteAND();
                    break;

                case "OR":
                    SetBasePointer();
                    stack.Push(opCode[1]);
                    sp++;
                    GetRegisterIndex();
                    
                    stack.Push(opCode[2]);
                    sp++;
                    GetRegisterIndex();

                    ExecuteOR();
                    break;

                case "XOR":
                    SetBasePointer();
                    stack.Push(opCode[1]);
                    sp++;
                    GetRegisterIndex();
                    
                    stack.Push(opCode[2]);
                    sp++;
                    GetRegisterIndex();

                    ExecuteXOR();
                    break;

                case "ADD":
                    SetBasePointer();
                    stack.Push(opCode[1]);
                    sp++;
                    GetRegisterIndex();
                    
                    stack.Push(opCode[2]);
                    sp++;
                    GetRegisterValue();

                    ExecuteADD();
                    break;

                case "SUB":
                    SetBasePointer();
                    stack.Push(opCode[2]);
                    sp++;
                    GetRegisterIndex();
                    
                    stack.Push(opCode[1]);
                    sp++;
                    GetRegisterValue();

                    ExecuteSUB();
                    break;

                case "SHR":
                    SetBasePointer();
                    stack.Push(opCode[2]);
                    sp++;
                    GetRegisterIndex();

                    SetBasePointer();
                    stack.Push(opCode[1]);
                    sp++;
                    GetRegisterIndex();

                    ExecuteSHR();
                    break;

                case "SHL":
                    SetBasePointer();
                    stack.Push(opCode[2]);
                    sp++;
                    GetRegisterIndex();

                    SetBasePointer();
                    stack.Push(opCode[1]);
                    sp++;
                    GetRegisterIndex();

                    ExecuteSHL();
                    break;

                case "RR":
                    SetBasePointer();
                    stack.Push(opCode[2]);
                    sp++;
                    GetRegisterIndex();
                    
                    stack.Push(opCode[1]);
                    sp++;
                    GetRegisterIndex();

                    ExecuteRR();
                    break;

                case "RL":
                    SetBasePointer();
                    stack.Push(opCode[2]);
                    sp++;
                    GetRegisterIndex();
                    
                    stack.Push(opCode[1]);
                    sp++;
                    GetRegisterIndex();

                    ExecuteRL();
                    break;

                case "RRC":
                    SetBasePointer();
                    stack.Push(opCode[2]);
                    sp++;
                    GetRegisterIndex();

                    stack.Push(opCode[1]);
                    sp++;
                    GetRegisterIndex();

                    ExecuteRRC();
                    break;

                case "RRL":
                    SetBasePointer();
                    stack.Push(opCode[2]);
                    sp++;
                    GetRegisterValue();

                    stack.Push(opCode[1]);
                    sp++;
                    GetRegisterValue();

                    ExecuteRRL();
                    break;
                default:
                    Error();
                    break;
            }
        }

        private static void ExecutePUSH()
        {
            // push value
            if (stack.Peek().ToString().Substring(0, 1).Equals("#"))
            {
                stack.Push(stack.Pop().ToString().Substring(1));
                sp++;
            }

            // push register and then get value
            else
            {
                stack.Push(stack.Pop().ToString());
                sp++;
                GetRegisterValue();
            }
        }

        private static void ExecutePOP()
        {
            // into register
            try
            {
                register[(int)stack.Pop()] = Convert.ToInt32(stack.Pop());
                sp--;
                sp--;
            }
            catch (Exception)
            {
                Error();
            }
        }

        private static void ExecuteRET()
        {
            try
            {
                pc = Convert.ToInt32(stack.Pop());
                sp--;
            }
            catch (Exception)
            {
                Error();
            }
        }

        private static void ExecuteJMP()
        {
            if (stack.Peek().ToString().Substring(0, 1).Equals("@"))
            {
                try
                {
                    pc = Convert.ToInt32(stack.Pop().ToString().Substring(1));
                    sp--;
                }
                catch (Exception)
                {
                    Error();
                }
            }
        }

        private static void ExecuteJR()
        {
            if (stack.Peek().ToString().Substring(0, 1).Equals("#"))
            {
                try
                {
                    pc += Convert.ToInt32(stack.Pop().ToString().Substring(1));
                    sp--;
                }
                catch (Exception)
                {
                    Error();
                }
            }
        }

        private static void ExecuteJRC()
        {
            if (stack.Peek().ToString().Substring(0, 1).Equals("#"))
            {
                if (flagC == 1)
                {
                    try
                    {
                        pc += Convert.ToInt32(stack.Pop().ToString().Substring(1));
                        sp--;
                    }
                    catch (Exception)
                    {
                        Error();
                    }
                }
                // pop jump inst
                else
                {
                    stack.Pop();
                    sp--;
                }
            }           
        }

        private static void ExecuteJRZ()
        {
            if (stack.Peek().ToString().Substring(0, 1).Equals("#"))
            {
                if (flagZ == 1)
                {
                    try
                    {
                        pc += Convert.ToInt32(stack.Pop().ToString().Substring(1));
                        sp--;
                    }
                    catch (Exception)
                    {
                        Error();
                    }
                }
                // pop jump inst
                else
                {
                    stack.Pop();
                    sp--;
                }
            }
        }

        private static void ExecuteJRN()
        {
            if (stack.Peek().ToString().Substring(0, 1).Equals("#"))
            {
                if (flagN == 1)
                {
                    try
                    {
                        pc += Convert.ToInt32(stack.Pop().ToString().Substring(1));
                        sp--;
                    }
                    catch (Exception)
                    {
                        Error();
                    }
                }
                // pop jump inst
                else
                {
                    stack.Pop();
                    sp--;
                }
            }            
        }

        private static void ExecuteAND()
        {
            register[8] = register[(int)stack.Pop()] & register[(int)stack.Pop()];
            sp--;
            sp--;
            register[0] = register[8];
            flagN = Convert.ToInt32(Convert.ToString(register[0], 2).PadLeft(32, '0').Substring(0, 1));

            if (register[0] == 0)
            {
                flagZ = 1;
            }
            else
            {
                flagZ = 0;
            }
        }

        private static void ExecuteOR()
        {
            register[8] = register[(int)stack.Pop()] | register[(int)stack.Pop()];
            sp--;
            sp--;
            register[0] = register[8];
            flagN = Convert.ToInt32(Convert.ToString(register[0], 2).PadLeft(32, '0').Substring(0, 1));

            if (register[0] == 0)
            {
                flagZ = 1;
            }
            else
            {
                flagZ = 0;
            }
        }

        private static void ExecuteXOR()
        {
            register[8] = register[(int)stack.Pop()] ^ register[(int)stack.Pop()];
            sp--;
            sp--;
            register[0] = register[8];
            flagN = Convert.ToInt32(Convert.ToString(register[0], 2).PadLeft(32, '0').Substring(0, 1));

            if (register[0] == 0)
            {
                flagZ = 1;
            }
            else
            {
                flagZ = 0;
            }           
        }

        private static void ExecuteADD()
        {
            try
            {
                register[8] = (int)stack.Pop();
                sp--;
                // checked throws an exception if overflow
                register[8] = checked(register[8] + register[(int)stack.Peek()]);
                stack.Pop();
                sp--;
                flagC = 0;
            }
            catch (Exception)
            {
                register[8] = register[8] + register[(int)stack.Pop()];
                sp--;
                flagC = 1;
            }

            register[0] = register[8];
            flagN = Convert.ToInt32(Convert.ToString(register[0], 2).PadLeft(32, '0').Substring(0, 1));

            if (register[0] == 0)
            {
                flagZ = 1;
            }
            else
            {
                flagZ = 0;
            }
        }

        private static void ExecuteSUB()
        {
            try
            {
                register[8] = (int)stack.Pop();                
                sp--;
                // checked throws an exception if overflow
                register[8] = checked(register[8] - register[(int)stack.Peek()]);
                stack.Pop();
                sp--;
                flagC = 0;
            }
            catch (Exception)
            {
                register[8] = register[8] - register[(int)stack.Pop()];
                sp--;
                flagC = 1;
            }

            register[0] = register[8];
            flagN = Convert.ToInt32(Convert.ToString(register[0], 2).PadLeft(32, '0').Substring(0, 1));

            if (register[0] == 0)
            {
                flagZ = 1;
            }
            else
            {
                flagZ = 0;
            }
        }

        private static void ExecuteSHR()
        {
            // shift, set carry, shift 1 more time
            register[8] = register[(int)stack.Pop()] >> (register[(int)stack.Pop()] - 1);
            sp--;
            sp--;
            flagC = Convert.ToInt32(Convert.ToString(register[8], 2).Substring(register[8].ToString().Length - 1));
            register[8] = register[8] >> 1;
            register[0] = register[8];
            flagN = Convert.ToInt32(Convert.ToString(register[0], 2).PadLeft(32, '0').Substring(0, 1));

            if (register[0] == 0)
            {
                flagZ = 1;
            }
            else
            {
                flagZ = 0;
            }
        }

        private static void ExecuteSHL()
        {
            // shift, set carry, shift 1 more time
            register[8] = (register[(int)stack.Pop()]) << register[(int)stack.Pop() - 1];
            sp--;
            sp--;
            flagC = Convert.ToInt32(Convert.ToString(register[8], 2).Substring(0, 1));
            register[8] = register[8] << 1;
            register[0] = register[8];
            flagN = Convert.ToInt32(Convert.ToString(register[0], 2).PadLeft(32, '0').Substring(0, 1));

            if (register[0] == 0)
            {
                flagZ = 1;
            }
            else
            {
                flagZ = 0;
            }
        }

        private static void ExecuteRR()
        {
            register[8] = (int)stack.Pop();
            sp--;

            //(original >> bits) | (original << (32 -bits))                    
            register[8] = (register[register[8]] >> register[(int)stack.Peek()] | (register[register[8]] << (32 - register[(int)stack.Pop()])));
            sp--;
            register[0] = register[8];
            flagN = Convert.ToInt32(Convert.ToString(register[0], 2).PadLeft(32, '0').Substring(0, 1));
            flagC = flagN;

            if (register[0] == 0)
            {
                flagZ = 1;
            }
            else
            {
                flagZ = 0;
            }
        }

        private static void ExecuteRL()
        {
            register[8] = (int)stack.Pop();
            sp--;

            // (original << bits) | (original >> (32 -bits))
            register[8] = (register[register[8]] << register[(int)stack.Peek()] | (register[register[8]] >> (32 - register[(int)stack.Pop()])));
            sp--;
            register[0] = register[8];
            flagN = Convert.ToInt32(Convert.ToString(register[0], 2).PadLeft(32, '0').Substring(0, 1));
            flagC = Convert.ToInt32(Convert.ToString(register[0], 2).PadLeft(32, '0').Substring(31));

            if (register[0] == 0)
            {
                flagZ = 1;
            }
            else
            {
                flagZ = 0;
            }
        }

        private static void ExecuteRRC()
        {
            register[8] = (int)stack.Pop();
            sp--;

            for (int i = 0; i < register[(int)stack.Pop()]; i++)
            {
                if (flagC == 0)
                {
                    flagC = Convert.ToInt32(Convert.ToString(register[8], 2).PadLeft(32, '0').Substring(31));
                    register[8] = register[8] >> 1;
                    register[0] = Convert.ToInt32(Convert.ToString(register[8], 2).PadLeft(32, '0').Substring(0, 1).Replace(flagC.ToString(), "0"));
                    register[8] = Convert.ToInt32(register[0].ToString(), 2);
                }
                else
                {
                    flagC = Convert.ToInt32(Convert.ToString(register[0], 2).PadLeft(32, '0').Substring(31));
                    register[8] = register[8] >> 1;
                    register[0] = Convert.ToInt32(Convert.ToString(register[8], 2).PadLeft(32, '0').Substring(0, 1).Replace(flagC.ToString(), "1"));
                    register[8] = register[0];
                }
            }

            sp--;
            register[0] = register[8];
            flagN = Convert.ToInt32(Convert.ToString(register[0], 2).PadLeft(32, '0').Substring(0, 1));

            if (register[0] == 0)
            {
                flagZ = 1;
            }
            else
            {
                flagZ = 0;
            }
        }

        private static void ExecuteRRL()
        {
            register[8] = (int)stack.Pop();
            sp--;

            for (int i = 0; i < (int)stack.Pop(); i++)
            {
                if (flagC == 0)
                {
                    flagC = Convert.ToInt32(Convert.ToString(register[8], 2).PadLeft(32, '0').Substring(31));
                    register[8] = register[8] >> 1;
                    register[0] = Convert.ToInt32(Convert.ToString(register[8], 2).PadLeft(32, '0').Substring(0, 1).Replace(flagC.ToString(), "0"));
                    register[8] = Convert.ToInt32(register[0].ToString(), 2);
                }
                else
                {
                    flagC = Convert.ToInt32(Convert.ToString(register[0], 2).PadLeft(32, '0').Substring(31));
                    register[8] = register[8] >> 1;
                    register[0] = Convert.ToInt32(Convert.ToString(register[8], 2).PadLeft(32, '0').Substring(0, 1).Replace(flagC.ToString(), "1"));
                    register[8] = register[0];
                }
            }

            sp--;
            register[0] = register[8];
            flagN = Convert.ToInt32(Convert.ToString(register[0], 2).PadLeft(32, '0').Substring(0, 1));

            if (register[0] == 0)
            {
                flagZ = 1;
            }
            else
            {
                flagZ = 0;
            }
        }

        private static void SetBasePointer()
        {
            bp = stack.Count;
        }

        private static void Error()
        {
            stack.Push(1);
            sp++;
            ExecuteHalt();
        }

        private static void ExecuteHalt()
        {
            ExecuteDumpRegisters();
            Environment.Exit((int)stack.Pop());
        }

        private static void GetRegisterValue()
        {
            string registerName = stack.Pop().ToString();
            sp--;
            int value = 0;

            switch (registerName)
            {
                case ("A"):
                    value = Convert.ToInt32(register[0]);
                    break;
                case ("B"):
                    value = Convert.ToInt32(register[1]);
                    break;
                case ("C"):
                    value = Convert.ToInt32(register[2]);
                    break;
                case ("D"):
                    value = Convert.ToInt32(register[3]);
                    break;
                case ("E"):
                    value = Convert.ToInt32(register[4]);
                    break;
                case ("F"):
                    value = Convert.ToInt32(register[5]);
                    break;
                case ("G"):
                    value = Convert.ToInt32(register[6]);
                    break;
                case ("H"):
                    value = Convert.ToInt32(register[7]);
                    break;
                case ("R"):
                    value = Convert.ToInt32(register[8]);
                    break;
                default:
                    Error();
                    break;
            }
            
            stack.Push(value);
            sp++;
        }

        private static void GetRegisterIndex()
        {
            string source = stack.Pop().ToString();
            sp--;
            int index = 0;

            switch (source)
            {
                case ("A"):
                    // index is already 0
                    break;
                case ("B"):
                    index = 1;
                    break;
                case ("C"):
                    index = 2;
                    break;
                case ("D"):
                    index = 3;
                    break;
                case ("E"):
                    index = 4;
                    break;
                case ("F"):
                    index = 5;
                    break;
                case ("G"):
                    index = 6;
                    break;
                case ("H"):
                    index = 7;
                    break;
                case ("R"):
                    index = 7;
                    break;
                default:
                    Error();
                    break;
            }
                        
            stack.Push(index);
            sp++;
        }

        private static void ExecuteDumpRegisters()
        {
            Console.WriteLine("Register A: {0}", register[0]);
            Console.WriteLine("Register B: {0}", register[1]);
            Console.WriteLine("Register C: {0}", register[2]);
            Console.WriteLine("Register D: {0}", register[3]);
            Console.WriteLine("Register E: {0}", register[4]);
            Console.WriteLine("Register F: {0}", register[5]);
            Console.WriteLine("Register G: {0}", register[6]);
            Console.WriteLine("Register H: {0}", register[7]);
        }

        private static void ExecuteMdump()
        {
            foreach (var item in ram)
            {
                Console.WriteLine("RAM: {0}", item.ToString());
            }
        }

        private static void ExecuteSdump()
        {
            foreach (var item in stack)
            {
                Console.WriteLine("STACK: {0}", item.ToString());
            }
        }

        private static void TraceFetch()
        {
            Console.WriteLine("Trace fetch: {0}", ir);
        }

        private static void TraceDecode()
        {
            Console.WriteLine("Trace decode: {0}", stack.Pop().ToString());
            sp--;
        }
    }
}