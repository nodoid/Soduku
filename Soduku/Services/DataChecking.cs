using CommunityToolkit.Mvvm.Messaging;
using Sudoku;
using Sudoku.Interfaces;
using Sudoku.Models;

namespace Suduku.Services
{
    public class DataChecking : IDataChecking
    {
#nullable enable
        IMessenger? messenger => Startup.ServiceProvider.GetService<IMessenger>();
#nullable disable

        bool useHint { get; set; }
        bool BruteForceStop = false;
        Stack<int[,]> ActualStack = new Stack<int[,]>();
        Stack<string[,]> PossibleStack = new Stack<string[,]>();
        int[,] Actual = new int[10, 10];
        string[,] Possible = new string[10, 10];
        Stack<string> Moves = new Stack<string> { };

        public void SetupVars(int[,] actual, string[,] poss, bool hint)
        {
            Actual = actual;
            Possible = poss;
            useHint = hint;
        }

        public string CalculatePossibleValues(int col, int row)
        {
            var str = Possible[col, row] == string.Empty ? "123456789" : Possible[col, row];
            
            for (var r = 1; r <= 9; r++)
            {
                if (Actual[col, r] != 0)
                {
                    str = str.Replace(Convert.ToInt32(Actual[col, r]).ToString(), string.Empty);
                }
            }

            for (var c = 1; c <= 9; c++)
            {
                if (Actual[c, row] != 0)
                {
                    str = str.Replace(Convert.ToInt32(Actual[c, row]).ToString(), string.Empty);
                }
            }
            
            var startC = col - ((col - 1) % 3);
            var startR = row - ((row - 1) % 3);

            for (var rr = startR; rr <= startR + 2; rr++)
            {
                for (var cc = startC; cc <= startC + 2; cc++)
                {
                    if (Actual[cc, rr] != 0)
                    {
                        str = str.Replace(Convert.ToInt32(Actual[cc, rr]).ToString(), string.Empty);
                    }
                }
            }

            return str == string.Empty ? "Invalid move" : str;
        }

        public bool CheckColumnsAndRows
        {
            get
            {
                bool changes = false;
                for (var row = 1; row <= 9; row++)
                {
                    for (var col = 1; col <= 9; col++)
                    {
                        if (Actual[col, row] == 0)
                        {
                            try
                            {
                                Possible[col, row] = CalculatePossibleValues(col, row);
                            }
                            catch (Exception ex)
                            {
                                messenger.Send(new StringMessage { Message = "Illegal move", Sender = "DataChecking" });
                                return false; // illegal move
                            }

                            messenger.Send(new TripleTupleMessage { Message = new Tuple<object, object, object>(col, row, Possible[col, row]), Sender = "ToolTip" });

                            if (Possible[col, row].Length == 1)
                            {
                                messenger.Send(new QuadTupleMessage 
                                { 
                                    Message = new Tuple<object, object, object, object>(col, row, Possible[col, row], 1), 
                                    Sender = "SetCell" });

                                Actual[col, row] = Convert.ToInt32(Possible[col, row]);

                                messenger.Send(new StringMessage { Message = "Col/Row and Minigrid Elimination", Sender = "DataActivity" });
                                messenger.Send(new StringMessage
                                {
                                    Message = $"Inserted value {Actual[col, row]} in ({col}, {row})",
                                    Sender = "DataActivity"
                                });

                                Moves.Push(col + row + Possible[col, row]);
                                changes = true;
                                if (useHint)
                                    return true;
                            }
                        }
                    }
                }
                return changes;
            }
        }

        public void FindCellWithFewestPossibleValues(ref int col, ref int row)
        {
            int min = 10;
            for (var r = 1; r <= 9; r++)
            {
                for (var c = 1; c <= 9; c++)
                {
                    if (Actual[c, r] == 0 && Possible[c, r].Length < min)
                    {
                        min = Possible[c, r].Length;
                        col = c;
                        row = r;
                    }
                }
            }
        }

        public bool IsMoveValid(int col, int row, int value)
        {
            bool rv = true;

            for (int r = 1; r <= 9; r++)
            {
                if (Actual[col, r] == value)
                {
                    rv = false;
                }
            }

            for (int c = 1; c <= 9; c++)
            {
                if (Actual[c, row] == value)
                {
                    rv = false;
                }
            }

            if (rv)
            {
                int startC = col - ((col - 1) % 3);
                int startR = row - ((row - 1) % 3);

                for (var rr = 0; rr <= 2; rr++)
                {
                    for (int cc = 0; cc <= 2; cc++)
                    {
                        if (Actual[startC + cc, startR + rr] == value)
                        {
                            rv = false;
                            break;
                        }
                    }
                }
            }
            return rv;
        }

        bool IsPuzzleSolved
        {
            get
            {
                var pattern = "123456789";
                var rv = true;

                for (var r = 1; r <= 9; r++)
                {
                    for (var c = 1; c <= 9; c++)
                    {
                        pattern = pattern.Replace(Convert.ToInt32(Actual[c, r]).ToString(), string.Empty);
                    }

                    rv = pattern.Length > 0;
                }

                for (var c = 1; c <= 9; c++)
                {
                    for (var r = 1; r <= 9; r++)
                    {
                        pattern = pattern.Replace(Convert.ToInt32(Actual[c, r]).ToString(), string.Empty);
                    }
                    rv = pattern.Length > 0;
                }

                if (rv)
                {
                    pattern = "123456789";
                    for (var c = 1; c <= 9; c += 3)
                    {
                        for (var r = 1; r <= 9; r += 3)
                        {
                            for (var cc = 0; cc <= 2; cc++)
                            {
                                for (var rr = 0; rr <= 2; rr++)
                                {
                                    pattern = pattern.Replace(Convert.ToInt32(Actual[c + cc, r + rr]).ToString(), string.Empty);
                                }
                            }
                        }

                        rv = pattern.Length > 0;
                    }
                }
                return rv;
            }
        }

        bool LookForLoneRangersInColumns
        {
            get
            {
                bool changes = false;
                int occurrence = 0;
                int cPos = 0;
                int rPos = 0;

                for (var c = 1; c <= 9; c++)
                {
                    for (var n = 1; n <= 9; n++)
                    {
                        occurrence = 0;
                        for (var r = 1; r <= 9; r++)
                        {
                            if (Actual[c, r] == 0 && Possible[c, r].Contains(n.ToString()))
                            {
                                occurrence += 1;
                                if (occurrence > 1)
                                    break; 
                                rPos = r;
                            }
                        }
                        if (occurrence == 1)
                        {
                            messenger.Send(new QuadTupleMessage
                            {
                                Message = new Tuple<object, object, object, object>(cPos, rPos, n, 1),
                                Sender = "SetCell"
                            });
                            messenger.Send(new TripleTupleMessage
                            {
                                Message = new Tuple<object, object, object>(cPos, rPos, n.ToString()),
                                Sender = "ToolTip"
                            });
                            Moves.Push(cPos + rPos + n.ToString());

                            messenger.Send(new StringMessage { Message = "Look for Lone Rangers in Columns", Sender = "DataActivity" });
                            messenger.Send(new StringMessage
                            {
                                Message = $"Inserted value {n} in ({cPos}, {rPos}",
                                Sender = "DataActivity"
                            });
                            
                            changes = true;
                            if (useHint)
                                return true;
                        }
                    }
                }
                return changes;
            }
        }

        bool LookForLoneRangersInMiniGrids
        {
            get 
            {
                bool changes = false;
                bool NextMiniGrid = false;
                int occurrence = 0;
                int cPos = 0;
                int rPos = 0;
                
                for (var n = 1; n <= 9; n++)
                {
                    for (var r = 1; r <= 9; r += 3)
                    {
                        for (var c = 1; c <= 9; c += 3)
                        {
                            NextMiniGrid = false;
                            occurrence = 0;
                            for (var rr = 0; rr <= 2; rr++)
                            {
                                for (var cc = 0; cc <= 2; cc++)
                                {
                                    if (Actual[c + cc, r + rr] == 0 && Possible[c + cc, r + rr].Contains(n.ToString()))
                                    {
                                        occurrence += 1;
                                        cPos = c + cc;
                                        rPos = r + rr;
                                        if (occurrence > 1)
                                        {
                                            NextMiniGrid = true;
                                            break;
                                        }
                                    }
                                }
                                if (NextMiniGrid)
                                    break; 
                            }
                            if ((!NextMiniGrid) && occurrence == 1)
                            {
                                messenger.Send(new QuadTupleMessage
                                {
                                    Message = new Tuple<object, object, object, object>(cPos, rPos, n, 1),
                                    Sender = "SetCell"
                                });
                                messenger.Send(new TripleTupleMessage
                                {
                                    Message = new Tuple<object, object, object>(cPos, rPos, n.ToString()),
                                    Sender = "ToolTip"
                                });

                                Moves.Push(cPos + rPos + n.ToString());

                                messenger.Send(new StringMessage { Message = "Look for Lone Rangers in Minigrids", Sender = "DataActivity" });
                                messenger.Send(new StringMessage { Message = $"Inserted value {n} in ({cPos}, {rPos}", 
                                    Sender = "DataActivity" });
                               
                                changes = true;
                                if (useHint)
                                    return true;
                            }
                        }
                    }
                }
                return changes;
            }
        }

        bool LookForLoneRangersInRows
        {
            get 
            {
                bool changes = false;
                int occurrence = 0;
                int cPos = 0;
                int rPos = 0;

                for (var r = 1; r <= 9; r++)
                {
                    for (var n = 1; n <= 9; n++)
                    {
                        occurrence = 0;
                        for (var c = 1; c <= 9; c++)
                        {
                            if (Actual[c, r] == 0 && Possible[c, r].Contains(n.ToString()))
                            {
                                occurrence += 1;
                                if (occurrence > 1)
                                    break;
                                cPos = c;
                                rPos = r;
                            }
                        }
                        if (occurrence == 1)
                        {
                            messenger.Send(new QuadTupleMessage
                            {
                                Message = new Tuple<object, object, object, object>(cPos, rPos, n, 1),
                                Sender = "SetCell"
                            });
                            messenger.Send(new TripleTupleMessage
                            {
                                Message = new Tuple<object, object, object>(cPos, rPos, n.ToString()),
                                Sender = "ToolTip"
                            });
                            Moves.Push(cPos + rPos + n.ToString());
                            messenger.Send(new StringMessage { Message = "Look for Lone Rangers in Rows", Sender = "DataActivity" });
                            messenger.Send(new StringMessage { Message = $"Inserted value {n} in ({cPos}, {rPos}", Sender = "DataActivity" });
                           
                            changes = true;
                            if (useHint)
                                return true;
                        }
                    }
                }
                return changes;
            }
        }

        public bool LookForTripletsInColumns
        {
            get 
            {
                bool changes = false;
                
                for (var c = 1; c <= 9; c++)
                {
                    for (var r = 1; r <= 9; r++)
                    {
                        if (Actual[c, r] == 0 && Possible[c, r].Length == 2)
                        {
                            for (var rr = r + 1; rr <= 9; rr++)
                            {
                                if (Possible[c, rr] == Possible[c, r])
                                {
                                    messenger.Send(new StringMessage { Message = $"Twins found in column at: ({c}, {r}) and ({c}, {rr})", Sender = "DataActivity" });
                                  
                                    for (var rrr = 1; rrr <= 9; rrr++)
                                    {
                                        if ((Actual[c, rrr] == 0) && (rrr != r) && (rrr != rr))
                                        {
                                            var original_possible = Possible[c, rrr];
                                            Possible[c, rrr] = Possible[c, rrr].Replace(Possible[c, r][0], Convert.ToChar(string.Empty));
                                            Possible[c, rrr] = Possible[c, rrr].Replace(Possible[c, r][1], Convert.ToChar(string.Empty));
                                            messenger.Send(new TripleTupleMessage
                                            {
                                                Message= new Tuple<object, object, object>(c, rrr, Possible[c, rrr]),
                                                Sender = "Tooltip"
                                            });

                                            if (original_possible != Possible[c, rrr])
                                            {
                                                changes = true;
                                            }

                                            if (Possible[c, rrr] == string.Empty)
                                            {
                                                messenger.Send(new StringMessage { Message = "Illegal move", Sender = "DataChecking" });
                                                return false;
                                            }

                                            if (Possible[c, rrr].Length == 1)
                                            {
                                                messenger.Send(new QuadTupleMessage
                                                {
                                                    Message = new Tuple<object, object, object, object>(c, rrr, Convert.ToInt32(Possible[c, rrr]), 1),
                                                    Sender = "SetCell"
                                                });
                                                messenger.Send(new TripleTupleMessage
                                                {
                                                    Message = new Tuple<object, object, object>(c, rrr, Possible[c, rrr]),
                                                    Sender = "ToolTip"
                                                });
                                                
                                                Moves.Push(c + rrr + Possible[c, rrr]);
                                                messenger.Send(new StringMessage { Message = "Looking for twins (by column)", Sender = "DataActivity" });
                                                messenger.Send(new StringMessage { Message = $"Inserted value {Actual[c, rrr]} in ({c}, {rr}", Sender = "DataActivity" });

                                                if (useHint)
                                                    return true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return changes;
            }
        }

        public bool LookForTripletsInMiniGrids
        {
            get 
            {
                bool changes = false;
                for (var r = 1; r <= 9; r++)
                {
                    for (var c = 1; c <= 9; c++)
                    {
                        if (Actual[c, r] == 0 && Possible[c, r].Length == 3)
                        {
                            var tripletsLocation = c.ToString() + r.ToString();

                            int startC = c - ((c - 1) % 3);
                            int startR = r - ((r - 1) % 3);

                            for (var rr = startR; rr <= startR + 2; rr++)
                            {
                                for (var cc = startC; cc <= startC + 2; cc++)
                                {
                                    if ((!((cc == c) && (rr == r))) && 
                                        ((Possible[cc, rr] == Possible[c, r]) || 
                                        (Possible[cc, rr].Length == 2 && Possible[c, r].Contains(Possible[cc, rr][0].ToString()) && 
                                        Possible[c, r].Contains(Possible[cc, rr][1].ToString()))))
                                    {
                                        tripletsLocation += cc.ToString() + rr.ToString();
                                    }
                                }
                            }

                            if (tripletsLocation.Length == 6)
                            {
                                messenger.Send(new StringMessage { Message = $"Triplets found in {tripletsLocation}", 
                                    Sender = "DataActivity" });
                                
                                for (var rrr = startR; rrr <= startR + 2; rrr++)
                                {
                                    for (int ccc = startC; ccc <= startC + 2; ccc++)
                                    {
                                        if (Actual[ccc, rrr] == 0 && 
                                            ccc != Convert.ToInt32(tripletsLocation[0].ToString()) && 
                                            rrr != Convert.ToInt32(tripletsLocation[1].ToString()) && 
                                            ccc != Convert.ToInt32(tripletsLocation[2].ToString()) && 
                                            rrr != Convert.ToInt32(tripletsLocation[3].ToString()) && 
                                            ccc != Convert.ToInt32(tripletsLocation[4].ToString()) && 
                                            rrr != Convert.ToInt32(tripletsLocation[5].ToString()))
                                        {
                                            var original_possible = Possible[ccc, rrr];
                                            Possible[ccc, rrr] = Possible[ccc, rrr].Replace(Possible[c, r][0], Convert.ToChar(string.Empty));
                                            Possible[ccc, rrr] = Possible[ccc, rrr].Replace(Possible[c, r][1], Convert.ToChar(string.Empty));
                                            Possible[ccc, rrr] = Possible[ccc, rrr].Replace(Possible[c, r][2], Convert.ToChar(string.Empty));
                                            messenger.Send(new TripleTupleMessage
                                            {
                                                Message = new Tuple<object, object, object>(ccc, rrr, Possible[ccc, rrr]),
                                                Sender = "ToolTip"
                                            });

                                            if (original_possible != Possible[ccc, rrr])
                                            {
                                                changes = true;
                                            }

                                            if (Possible[ccc, rrr] == string.Empty)
                                            {
                                                messenger.Send(new StringMessage { Message = "Illegal move", Sender = "DataChecking" });
                                                return false;
                                            }

                                            if (Possible[ccc, rrr].Length == 1)
                                            {
                                                messenger.Send(new QuadTupleMessage
                                                {
                                                    Message = new Tuple<object, object, object, object>(ccc, rrr, 
                                                    Convert.ToInt32(Possible[ccc, rrr]), 1),
                                                    Sender = "SetCell"
                                                });
                                                messenger.Send(new TripleTupleMessage
                                                {
                                                    Message = new Tuple<object, object, object>(ccc, rrr, Possible[ccc, rrr]),
                                                    Sender = "ToolTip"
                                                });
                                                
                                                Moves.Push(ccc + rrr + Possible[ccc, rrr]);
                                                messenger.Send(new StringMessage { Message = "Look For Triplets in Minigrids", 
                                                    Sender = "DataActivity" });
                                                messenger.Send(new StringMessage { 
                                                    Message = $"Inserted value {Actual[ccc, rrr]} in ({ccc}, {rrr}", 
                                                    Sender = "DataActivity" });
                                                
                                                if (useHint)
                                                    return true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return changes;
            }
        }

        bool LookForTripletsInRows
        {
            get
            {
                var changes = false;

                for (var r = 1; r <= 9; r++)
                {

                    for (var c = 1; c <= 9; c++)
                    {
                        if (Actual[c, r] == 0 && Possible[c, r].Length == 3)
                        {
                            var tripletsLocation = c.ToString() + r.ToString();

                            for (var cc = 1; cc <= 9; cc++)
                            {
                                if ((cc != c) && ((Possible[cc, r] == Possible[c, r]) || (Possible[cc, r].Length == 2
                                    && Possible[c, r].Contains(Possible[cc, r][0].ToString())
                                    && Possible[c, r].Contains(Possible[cc, r][1].ToString()))))
                                {
                                    tripletsLocation += cc.ToString() + r.ToString();
                                }
                            }

                            if (tripletsLocation.Length == 6)
                            {
                                messenger.Send(new StringMessage { Message = $"Triplets found in {tripletsLocation}", 
                                    Sender = "DataActivity" });

                                for (var ccc = 1; ccc <= 9; ccc++)
                                {
                                    if (Actual[ccc, r] == 0 && ccc != Convert.ToInt32(tripletsLocation[0].ToString())
                                        && ccc != Convert.ToInt32(tripletsLocation[2].ToString())
                                        && ccc != Convert.ToInt32(tripletsLocation[4].ToString()))
                                    {
                                        var original_possible = Possible[ccc, r];
                                        Possible[ccc, r] = Possible[ccc, r].Replace(Possible[c, r][0], Convert.ToChar(string.Empty));
                                        Possible[ccc, r] = Possible[ccc, r].Replace(Possible[c, r][1], Convert.ToChar(string.Empty));
                                        Possible[ccc, r] = Possible[ccc, r].Replace(Possible[c, r][2], Convert.ToChar(string.Empty));
                                        messenger.Send(new TripleTupleMessage
                                        {
                                            Message = new Tuple<object, object, object>(ccc, r, Possible[ccc, r]),
                                            Sender = "ToolTip"
                                        });

                                        if (original_possible != Possible[ccc, r])
                                        {
                                            changes = true;
                                        }

                                        if (Possible[ccc, r] == string.Empty)
                                        {
                                            messenger.Send(new StringMessage { Message = "Illegal move", Sender = "DataChecking" });
                                            return false;
                                        }

                                        if (Possible[ccc, r].Length == 1)
                                        {
                                            messenger.Send(new QuadTupleMessage
                                            {
                                                Message = new Tuple<object, object, object, object>(ccc, r,
                                                Convert.ToInt32(Possible[ccc, r]), 1),
                                                Sender = "SetCell"
                                            }); 
                                            messenger.Send(new TripleTupleMessage
                                            {
                                                Message = new Tuple<object, object, object>(ccc, r, Possible[ccc, r]),
                                                Sender = "ToolTip"
                                            });

                                            Moves.Push(ccc + r + Possible[ccc, r]);
                                            messenger.Send(new StringMessage { Message = "Look For Triplets in Rows", Sender = "DataActivity" });
                                            messenger.Send(new StringMessage { Message = $"Inserted value {Actual[ccc, r]} in ({ccc}, {r})", Sender = "DataActivity" });

                                            if (useHint)
                                                return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return changes;
            }
        }

        public bool LookForTwinsInColumns
        {
            get 
            {
                bool changes = false;
                for (var c = 1; c <= 9; c++)
                {
                    for (var r = 1; r <= 9; r++)
                    {
                        if (Actual[c, r] == 0 && Possible[c, r].Length == 2)
                        {
                            for (var rr = r + 1; rr <= 9; rr++)
                            {
                                if (Possible[c, rr] == Possible[c, r])
                                {
                                    messenger.Send(new StringMessage
                                    {
                                        Message = $"Twins found in column at ({c}, {r}) and ({c}, {rr})",
                                        Sender = "DataActivity"
                                    });
                                    
                                    for (var rrr = 1; rrr <= 9; rrr++)
                                    {
                                        if ((Actual[c, rrr] == 0) && (rrr != r) && (rrr != rr))
                                        {
                                            var original_possible = Possible[c, rrr];
                                            Possible[c, rrr] = Possible[c, rrr].Replace(Possible[c, r][0], Convert.ToChar(string.Empty));
                                            Possible[c, rrr] = Possible[c, rrr].Replace(Possible[c, r][1], Convert.ToChar(string.Empty));
                                            messenger.Send(new TripleTupleMessage
                                            {
                                                Message = new Tuple<object, object, object>(c, rrr, Possible[c, rrr]),
                                                Sender = "ToolTip"
                                            });
                                            
                                            if (original_possible != Possible[c, rrr])
                                            {
                                                changes = true;
                                            }

                                            if (Possible[c, rrr] == string.Empty)
                                            {
                                                messenger.Send(new StringMessage { Message = "Illegal move", Sender = "DataChecking" });
                                                return false;
                                            }

                                            if (Possible[c, rrr].Length == 1)
                                            {
                                                messenger.Send(new QuadTupleMessage
                                                {
                                                    Message = new Tuple<object, object, object, object>(c, rrr, 
                                                    Convert.ToInt32(Possible[c, rrr]), 1),
                                                    Sender = "SetCell"
                                                });
                                                messenger.Send(new TripleTupleMessage
                                                {
                                                    Message = new Tuple<object, object, object>(c, rrr, Possible[c, rrr]),
                                                    Sender = "ToolTip"
                                                });
                                                
                                                Moves.Push(c + rrr + Possible[c, rrr]);
                                                messenger.Send(new StringMessage { Message = "Looking for twins (by column)", 
                                                    Sender = "DataActivity" });
                                                messenger.Send(new StringMessage 
                                                { 
                                                    Message = $"Inserted value {Actual[c, rrr]} in ({c}, {rrr})", 
                                                    Sender = "DataActivity" 
                                                });

                                                if (useHint)
                                                    return true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return changes;
            }
        }

        public bool LookForTwinsInMiniGrids
        {
            get 
            {
                bool changes = false;

                for (var r = 1; r <= 9; r++)
                {
                    for (var c = 1; c <= 9; c++)
                    {
                        if (Actual[c, r] == 0 && Possible[c, r].Length == 2)
                        {
                            var startC = c - ((c - 1) % 3);
                            var startR = r - ((r - 1) % 3);
                            
                            for (var rr = startR; rr <= startR + 2; rr++)
                            {
                                for (int cc = startC; cc <= startC + 2; cc++)
                                {
                                    if ((!((cc == c) && (rr == r))) && Possible[cc, rr] == Possible[c, r])
                                    {
                                        messenger.Send(new StringMessage
                                        {
                                            Message = $"Twins found in minigrid at ({c}, {r}) and ({c}, {rr})",
                                            Sender = "DataActivity"
                                        });
                                        
                                        for (var rrr = startR; rrr <= startR + 2; rrr++)
                                        {
                                            for (var ccc = startC; ccc <= startC + 2; ccc++)
                                            {
                                                if (Actual[ccc, rrr] == 0 && Possible[ccc, rrr] != Possible[c, r])
                                                {
                                                    var original_possible = Possible[ccc, rrr];
                                                    Possible[ccc, rrr] = Possible[ccc, rrr].Replace(Possible[c, r][0], 
                                                        Convert.ToChar(string.Empty));
                                                    Possible[ccc, rrr] = Possible[ccc, rrr].Replace(Possible[c, r][1],
                                                        Convert.ToChar(string.Empty));
                                                    messenger.Send(new TripleTupleMessage
                                                    {
                                                        Message = new Tuple<object, object, object>(ccc, rrr, Possible[ccc, rrr]),
                                                        Sender = "ToolTip"
                                                    });
                                                    
                                                    if (original_possible != Possible[ccc, rrr])
                                                    {
                                                        changes = true;
                                                    }

                                                    if (Possible[ccc, rrr] == string.Empty)
                                                    {
                                                        messenger.Send(new StringMessage { Message = "Illegal move", Sender = "DataChecking" });
                                                        return false;
                                                    }

                                                    if (Possible[ccc, rrr].Length == 1)
                                                    {
                                                        messenger.Send(new QuadTupleMessage
                                                        {
                                                            Message = new Tuple<object, object, object, object>(ccc, rrr, 
                                                            Convert.ToInt32(Possible[ccc, rrr]), 1),
                                                            Sender = "SetCell"
                                                        });
                                                        messenger.Send(new TripleTupleMessage
                                                        {
                                                            Message = new Tuple<object, object, object>(ccc, rrr, Possible[ccc, rrr]),
                                                            Sender = "ToolTip"
                                                        });
                                                        
                                                        Moves.Push(ccc + rrr + Possible[ccc, rrr]);
                                                        messenger.Send(new StringMessage
                                                        {
                                                            Message = "Looking for twins in Minigrids",
                                                            Sender = "DataActivity"
                                                        });
                                                        messenger.Send(new StringMessage
                                                        {
                                                            Message = $"Inserted value {Actual[ccc, rrr]} in ({ccc}, {rrr})",
                                                            Sender = "DataActivity"
                                                        });
                                                        
                                                        if (useHint)
                                                            return true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return changes;
            }
        }

        public bool LookForTwinsInRows
        {
            get 
            {
                bool changes = false;

                for (var r = 1; r <= 9; r++)
                {
                    for (var c = 1; c <= 9; c++)
                    {
                        if (Actual[c, r] == 0 && Possible[c, r].Length == 2)
                        {
                            for (var cc = c + 1; cc <= 9; cc++)
                            {
                                if (Possible[cc, r] == Possible[c, r])
                                {
                                    messenger.Send(new StringMessage
                                    {
                                        Message = $"Twins found in row at ({c}, {r}) and ({cc}, {r})",
                                        Sender = "DataActivity"
                                    });
                                    
                                    for (var ccc = 1; ccc <= 9; ccc++)
                                    {
                                        if ((Actual[ccc, r] == 0) && (ccc != c) && (ccc != cc))
                                        {
                                            var original_possible = Possible[ccc, r];
                                            Possible[ccc, r] = Possible[ccc, r].Replace(Possible[c, r][0], Convert.ToChar(string.Empty));
                                            Possible[ccc, r] = Possible[ccc, r].Replace(Possible[c, r][1], Convert.ToChar(string.Empty));
                                            messenger.Send(new TripleTupleMessage
                                            {
                                                Message = new Tuple<object, object, object>(ccc, r, Possible[ccc, r]),
                                                Sender = "ToolTip"
                                            });

                                            if (original_possible != Possible[ccc, r])
                                            {
                                                changes = true;
                                            }

                                            if (Possible[ccc, r] == string.Empty)
                                            {
                                                messenger.Send(new StringMessage { Message = "Illegal move", Sender = "DataChecking" });
                                                return false;
                                            }

                                            if (Possible[ccc, r].Length == 1)
                                            {
                                                messenger.Send(new QuadTupleMessage
                                                {
                                                    Message = new Tuple<object, object, object, object>(ccc, r, Convert.ToInt32(Possible[ccc, r]), 1),
                                                    Sender = "SetCell"
                                                });
                                                messenger.Send(new TripleTupleMessage
                                                {
                                                    Message = new Tuple<object, object, object>(ccc, r, Possible[ccc, r]),
                                                    Sender = "ToolTip"
                                                });

                                                Moves.Push(ccc + r + Possible[ccc, r]);

                                                messenger.Send(new StringMessage
                                                {
                                                    Message = "Looking for twins in rows",
                                                    Sender = "DataActivity"
                                                });
                                                messenger.Send(new StringMessage
                                                {
                                                    Message = $"Inserted value {Actual[ccc, r]} in ({ccc}, {r})",
                                                    Sender = "DataActivity"
                                                });
                                                
                                                if (useHint)
                                                    return true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return changes;
            }
        }

        public void RandomizePossibleValues(ref string str)
        {
            var s = new char[str.Length];
            var temp = '\0';
            s = str.ToCharArray();
            var rand = new Random().Next();
            for (var i = 0; i <= str.Length - 1; i++)
            {
                var j = Convert.ToInt32((str.Length - i + 1) * rand + i) % str.Length;
                temp = s[i];
                s[i] = s[j];
                s[j] = temp;
            }
            str = s.ToString();
        }

        public bool SolvePuzzle
        {
            get
            {
                bool _ = false;
                try
                {
                    do
                    {
                        do
                        {
                            do
                            {
                                do
                                {
                                    do
                                    {
                                        do
                                        {
                                            do
                                            {
                                                do
                                                {
                                                    do
                                                    {
                                                        do
                                                        {
                                                            _ = (useHint && CheckColumnsAndRows) || IsPuzzleSolved;
                                                        } while (!!CheckColumnsAndRows);

                                                        if (_)
                                                            break;

                                                        _ = (useHint && LookForLoneRangersInMiniGrids) || IsPuzzleSolved;
                                                    } while (!!LookForLoneRangersInMiniGrids);

                                                    if (_)
                                                        break;

                                                    _ = (useHint && LookForLoneRangersInRows) || IsPuzzleSolved;
                                                } while (!!LookForLoneRangersInRows);

                                                if (_)
                                                    break;

                                                _ = (useHint && LookForLoneRangersInColumns) || IsPuzzleSolved;
                                            } while (!!LookForLoneRangersInColumns);

                                            if (_)
                                                break;

                                            _ = (useHint && LookForTwinsInMiniGrids) || IsPuzzleSolved;
                                        } while (!!LookForTwinsInMiniGrids);

                                        if (_)
                                            break;

                                        _ = (useHint && LookForTwinsInRows) || IsPuzzleSolved;
                                    } while (!!LookForTwinsInRows);

                                    if (_)
                                        break;

                                    _ = (useHint && LookForTwinsInColumns) || IsPuzzleSolved;
                                } while (!!LookForTwinsInColumns);

                                if (_)
                                    break;

                                _ = (useHint && LookForTripletsInMiniGrids) || IsPuzzleSolved;
                            } while (!!LookForTripletsInMiniGrids);

                            if (_)
                                break;

                            _ = (useHint && LookForTripletsInRows) || IsPuzzleSolved;
                        } while (!!LookForTripletsInRows);

                        if (_)
                            break;

                        _ = (useHint && LookForTripletsInColumns) || IsPuzzleSolved;
                    } while (!!LookForTripletsInColumns);
                }
                catch (Exception ex)
                {
                    messenger.Send(new StringMessage { Message = "Illegal move", Sender = "DataChecking" });
                    return false;
                }

                return IsPuzzleSolved;
            }
        }

        public void SolvePuzzleByBruteForce()
        {
            int c = 0;
            int r = 0;

            FindCellWithFewestPossibleValues(ref c, ref r);
            var possibleValues = Possible[c, r];

            ActualStack.Push((int[,])Actual.Clone());
            PossibleStack.Push((string[,])Possible.Clone());

            for (var i = 0; i <= possibleValues.Length - 1; i++)
            {
                Moves.Push(c + r + possibleValues[i].ToString());
                messenger.Send(new QuadTupleMessage
                {
                    Message = new Tuple<object, object, object, object>(c, r, Convert.ToInt32(possibleValues[i].ToString()), 1),
                    Sender = "SetCell"
                });

                messenger.Send(new StringMessage
                {
                    Message = "Solve Puzzle By Brute Force",
                    Sender = "DataActivity"
                });
                messenger.Send(new StringMessage
                {
                    Message = $"Trying to insert value  {Actual[c, r]} in ({c}, {r})",
                    Sender = "DataActivity"
                });

                try
                {
                    if (SolvePuzzle)
                    {
                        BruteForceStop = true;
                        return;
                    }
                    else
                    {
                        SolvePuzzleByBruteForce();
                        if (BruteForceStop)
                            return;
                    }
                }
                catch (Exception ex)
                {
                    messenger.Send(new StringMessage { Message = "Illegal move; Backtracking...", Sender = "DataChecking" });
                    Actual = ActualStack.Pop();
                    Possible = PossibleStack.Pop();
                }
            }
        }
    }
}
