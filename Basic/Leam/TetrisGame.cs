using System;
using System.Collections.Generic;
using System.Threading;

namespace ConsoleTetrisOOP
{
    public class Program
    {
        public interface IRollable
        {
            void Rotate(int[,] board);
        }

        public abstract class Block
        {
            public string Name { get; protected set; }
            public int[,] Shape { get; protected set; }
            public int X { get; set; }
            public int Y { get; set; }
            public ConsoleColor Color { get; protected set; }

            protected Block(string name, int[,] shape, ConsoleColor color)
            {
                Name = name;
                Shape = shape;
                Color = color;
                X = 3;
                Y = 0;
            }

            public virtual void Move(int dx, int dy, int[,] board)
            {
                if (!CheckCollision(X + dx, Y + dy, Shape, board))
                {
                    X += dx;
                    Y += dy;
                }
            }

            public bool CanFall(int[,] board)
            {
                return !CheckCollision(X, Y + 1, Shape, board);
            }

            public virtual void OnUpdate(int[,] board)
            {
            }

            public static bool CheckCollision(int targetX, int targetY, int[,] targetShape, int[,] board)
            {
                int rows = targetShape.GetLength(0);
                int cols = targetShape.GetLength(1);
                int boardHeight = board.GetLength(0);
                int boardWidth = board.GetLength(1);

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (targetShape[r, c] != 0)
                        {
                            int newX = targetX + c;
                            int newY = targetY + r;

                            if (newX < 0 || newX >= boardWidth || newY >= boardHeight)
                                return true;

                            if (newY >= 0 && board[newY, newX] != 0)
                                return true;
                        }
                    }
                }
                return false;
            }
        }

        public class IBlock : Block, IRollable
        {
            public IBlock() : base("I", new int[,]
            {
                { 0, 0, 0, 0 },
                { 1, 1, 1, 1 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 }
            }, ConsoleColor.Cyan)
            {
            }

            public void Rotate(int[,] board)
            {
                int n = Shape.GetLength(0);
                int[,] rotated = new int[n, n];
                for (int r = 0; r < n; r++)
                    for (int c = 0; c < n; c++)
                        rotated[c, n - 1 - r] = Shape[r, c];

                if (!CheckCollision(X, Y, rotated, board))
                {
                    Shape = rotated;
                }
            }
        }

        public class TBlock : Block, IRollable
        {
            public TBlock() : base("T", new int[,]
            {
                { 0, 1, 0 },
                { 1, 1, 1 },
                { 0, 0, 0 }
            }, ConsoleColor.Magenta)
            {
            }

            public void Rotate(int[,] board)
            {
                int n = Shape.GetLength(0);
                int[,] rotated = new int[n, n];
                for (int r = 0; r < n; r++)
                    for (int c = 0; c < n; c++)
                        rotated[c, n - 1 - r] = Shape[r, c];

                if (!CheckCollision(X, Y, rotated, board))
                {
                    Shape = rotated;
                }
            }
        }

        public class OBlock : Block
        {
            public OBlock() : base("O", new int[,]
            {
                { 1, 1 },
                { 1, 1 }
            }, ConsoleColor.Yellow)
            {
                X = 4;
            }
        }

        public class TimerBlock : Block, IRollable
        {
            public int DeathTimer { get; private set; }
            public bool IsExpired => DeathTimer <= 0;
            private DateTime lastTick;

            public TimerBlock() : base("Timer", new int[,]
            {
                { 1, 1, 0 },
                { 0, 1, 1 },
                { 0, 0, 0 }
            }, ConsoleColor.Red)
            {
                DeathTimer = 5;
                lastTick = DateTime.Now;
            }

            public override void OnUpdate(int[,] board)
            {
                if ((DateTime.Now - lastTick).TotalSeconds >= 1.0)
                {
                    DeathTimer--;
                    lastTick = DateTime.Now;
                }
            }

            public void Rotate(int[,] board)
            {
                int n = Shape.GetLength(0);
                int[,] rotated = new int[n, n];
                for (int r = 0; r < n; r++)
                    for (int c = 0; c < n; c++)
                        rotated[c, n - 1 - r] = Shape[r, c];

                if (!CheckCollision(X, Y, rotated, board))
                {
                    Shape = rotated;
                }
            }
        }

        public class TetrisGame
        {
            private const int Width = 10;
            private const int Height = 20;
            private int[,] board = new int[Height, Width];
            private Block currentBlock;
            private Random random = new Random();
            private int score = 0;
            private bool isGameOver = false;
            private DateTime lastDropTime = DateTime.Now;
            private int dropIntervalMs = 500;

            public void Run()
            {
                Console.CursorVisible = false;
                SpawnBlock();

                while (!isGameOver)
                {
                    ProcessInput();
                    Update();
                    Render();
                    Thread.Sleep(30);
                }

                Console.SetCursorPosition(0, Height + 4);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"GAME OVER! 최종 점수: {score}");
                Console.ResetColor();
            }

            private void SpawnBlock()
            {
                int pick = random.Next(4);
                switch (pick)
                {
                    case 0: currentBlock = new IBlock(); break;
                    case 1: currentBlock = new TBlock(); break;
                    case 2: currentBlock = new OBlock(); break;
                    default: currentBlock = new TimerBlock(); break;
                }

                if (Block.CheckCollision(currentBlock.X, currentBlock.Y, currentBlock.Shape, board))
                {
                    isGameOver = true;
                }
            }

            private void ProcessInput()
            {
                if (!Console.KeyAvailable) return;

                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.LeftArrow:
                        currentBlock.Move(-1, 0, board);
                        break;
                    case ConsoleKey.RightArrow:
                        currentBlock.Move(1, 0, board);
                        break;
                    case ConsoleKey.DownArrow:
                        currentBlock.Move(0, 1, board);
                        break;
                    case ConsoleKey.UpArrow:
                        if (currentBlock is IRollable rollable)
                        {
                            rollable.Rotate(board);
                        }
                        break;
                    case ConsoleKey.Spacebar:
                        while (currentBlock.CanFall(board))
                        {
                            currentBlock.Move(0, 1, board);
                        }
                        LockBlock();
                        break;
                }
            }

            private void Update()
            {
                currentBlock.OnUpdate(board);

                if (currentBlock is TimerBlock timerBlock && timerBlock.IsExpired)
                {
                    SpawnBlock();
                    return;
                }

                if ((DateTime.Now - lastDropTime).TotalMilliseconds >= dropIntervalMs)
                {
                    if (currentBlock.CanFall(board))
                    {
                        currentBlock.Move(0, 1, board);
                    }
                    else
                    {
                        LockBlock();
                    }
                    lastDropTime = DateTime.Now;
                }
            }

            private void LockBlock()
            {
                int rows = currentBlock.Shape.GetLength(0);
                int cols = currentBlock.Shape.GetLength(1);

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (currentBlock.Shape[r, c] != 0)
                        {
                            int bx = currentBlock.X + c;
                            int by = currentBlock.Y + r;
                            if (by >= 0 && by < Height && bx >= 0 && bx < Width)
                            {
                                board[by, bx] = 1;
                            }
                        }
                    }
                }

                ClearFullLines();
                SpawnBlock();
            }

            private void ClearFullLines()
            {
                for (int r = Height - 1; r >= 0; r--)
                {
                    bool isFull = true;
                    for (int c = 0; c < Width; c++)
                    {
                        if (board[r, c] == 0)
                        {
                            isFull = false;
                            break;
                        }
                    }

                    if (isFull)
                    {
                        score += 100;
                        for (int moveRow = r; moveRow > 0; moveRow--)
                        {
                            for (int c = 0; c < Width; c++)
                            {
                                board[moveRow, c] = board[moveRow - 1, c];
                            }
                        }
                        for (int c = 0; c < Width; c++)
                        {
                            board[0, c] = 0;
                        }
                        r++;
                    }
                }
            }

            private void Render()
            {
                Console.SetCursorPosition(0, 0);

                int[,] displayBuffer = (int[,])board.Clone();
                int rows = currentBlock.Shape.GetLength(0);
                int cols = currentBlock.Shape.GetLength(1);

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (currentBlock.Shape[r, c] != 0)
                        {
                            int bx = currentBlock.X + c;
                            int by = currentBlock.Y + r;
                            if (by >= 0 && by < Height && bx >= 0 && bx < Width)
                            {
                                displayBuffer[by, bx] = 2;
                            }
                        }
                    }
                }

                for (int r = 0; r < Height; r++)
                {
                    Console.Write("<!");
                    for (int c = 0; c < Width; c++)
                    {
                        if (displayBuffer[r, c] == 1)
                        {
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.Write("■");
                            Console.ResetColor();
                        }
                        else if (displayBuffer[r, c] == 2)
                        {
                            Console.ForegroundColor = currentBlock.Color;
                            Console.Write("■");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.Write(" .");
                        }
                    }
                    Console.WriteLine("!>");
                }
                Console.WriteLine("<!====================!>");
                Console.WriteLine($"점수: {score} | 현재 블록: {currentBlock.Name}   ");

                if (currentBlock is TimerBlock tb)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[경고] 폭탄 블록 소멸까지: {tb.DeathTimer}초   ");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("                                      ");
                }
            }
        }

        public static void Main(string[] args)
        {
            TetrisGame game = new TetrisGame();
            game.Run();
        }
    }
}
