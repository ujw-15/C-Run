using System;
using System.Collections.Generic;
using System.Threading;

namespace ConsoleTetris
{
    public class Program
    {
        public interface IRollable
        {
            void Rotate(int[,] board);
        }

        public interface IBoardObstacle
        {
            void OnBoardTick(int[,] board, Block currentBlock, ref bool isGameOver);
            void RenderPreview(int[,] board, ConsoleColor[,] previewColors);
        }

        public abstract class Block
        {
            public string ShapeName { get; protected set; }
            public int[,] Matrix { get; protected set; }
            public int X { get; set; }
            public int Y { get; set; }
            public ConsoleColor Color { get; protected set; }

            protected Block(string shapeName, int[,] matrix, ConsoleColor color)
            {
                ShapeName = shapeName;
                Matrix = matrix;
                Color = color;
                X = 3;
                Y = 0;
            }

            public virtual void OnUpdate(double deltaTime, ref bool isVanished)
            {
            }

            public virtual string GetStatusText()
            {
                return $"현재 블록: {ShapeName}";
            }

            public bool CanMove(int[,] board, int offsetX, int offsetY, int[,] testMatrix = null)
            {
                int[,] target = testMatrix ?? Matrix;
                int rows = target.GetLength(0);
                int cols = target.GetLength(1);
                int boardHeight = board.GetLength(0);
                int boardWidth = board.GetLength(1);

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (target[r, c] == 0) continue;

                        int newX = X + c + offsetX;
                        int newY = Y + r + offsetY;

                        if (newX < 0 || newX >= boardWidth || newY < 0 || newY >= boardHeight)
                            return false;

                        if (board[newY, newX] != 0)
                            return false;
                    }
                }
                return true;
            }

            public bool Move(int[,] board, int offsetX, int offsetY)
            {
                if (CanMove(board, offsetX, offsetY))
                {
                    X += offsetX;
                    Y += offsetY;
                    return true;
                }
                return false;
            }

            public bool CanFall(int[,] board)
            {
                return CanMove(board, 0, 1);
            }

            protected int[,] GetRotatedMatrix()
            {
                int rows = Matrix.GetLength(0);
                int cols = Matrix.GetLength(1);
                int[,] rotated = new int[cols, rows];

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        rotated[c, rows - 1 - r] = Matrix[r, c];
                    }
                }
                return rotated;
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
            }, ConsoleColor.Cyan) { }

            public void Rotate(int[,] board)
            {
                int[,] rotated = GetRotatedMatrix();
                if (CanMove(board, 0, 0, rotated))
                {
                    Matrix = rotated;
                }
            }
        }

        public class OBlock : Block
        {
            public OBlock() : base("O (회전불가)", new int[,]
            {
                { 1, 1 },
                { 1, 1 }
            }, ConsoleColor.Yellow) { }
        }

        public class TBlock : Block, IRollable
        {
            public TBlock() : base("T", new int[,]
            {
                { 0, 1, 0 },
                { 1, 1, 1 },
                { 0, 0, 0 }
            }, ConsoleColor.Magenta) { }

            public void Rotate(int[,] board)
            {
                int[,] rotated = GetRotatedMatrix();
                if (CanMove(board, 0, 0, rotated))
                {
                    Matrix = rotated;
                }
            }
        }

        public class LBlock : Block, IRollable
        {
            public LBlock() : base("L", new int[,]
            {
                { 0, 0, 1 },
                { 1, 1, 1 },
                { 0, 0, 0 }
            }, ConsoleColor.DarkYellow) { }

            public void Rotate(int[,] board)
            {
                int[,] rotated = GetRotatedMatrix();
                if (CanMove(board, 0, 0, rotated))
                {
                    Matrix = rotated;
                }
            }
        }

        public class TimerBlock : Block, IRollable
        {
            public double DeathTimer { get; private set; }

            public TimerBlock(double seconds = 5.0) : base("Timer-Z", new int[,]
            {
                { 1, 1, 0 },
                { 0, 1, 1 },
                { 0, 0, 0 }
            }, ConsoleColor.Red)
            {
                DeathTimer = seconds;
            }

            public override void OnUpdate(double deltaTime, ref bool isVanished)
            {
                DeathTimer -= deltaTime;
                if (DeathTimer <= 0)
                {
                    TimeDeath(ref isVanished);
                }
            }

            public void TimeDeath(ref bool isVanished)
            {
                DeathTimer = 0;
                isVanished = true;
            }

            public override string GetStatusText()
            {
                return $"현재 블록: {ShapeName} (소멸까지 {Math.Max(0, DeathTimer):F1}초)";
            }

            public void Rotate(int[,] board)
            {
                int[,] rotated = GetRotatedMatrix();
                if (CanMove(board, 0, 0, rotated))
                {
                    Matrix = rotated;
                }
            }
        }

        public class UpFloor : Block, IBoardObstacle
        {
            private readonly Random _random = new Random();
            private readonly double _spawnInterval;
            private double _elapsedTime;

            public int NextStartX { get; private set; }
            public int NextWidth { get; private set; }
            public int NextHeight { get; private set; }
            public int[,] NextPattern { get; private set; }

            public UpFloor(double intervalSeconds = 8.0) : base("UpFloor", new int[1, 1], ConsoleColor.DarkGray)
            {
                _spawnInterval = intervalSeconds;
                _elapsedTime = 0;
                PrepareNextWarning(10);
            }

            public void PrepareNextWarning(int boardWidth)
            {
                NextWidth = _random.Next(1, 4);  // 최대 3 (가로)
                NextHeight = _random.Next(1, 4); // 최대 3 (세로)
                NextStartX = _random.Next(0, boardWidth - NextWidth + 1);
                NextPattern = new int[NextHeight, NextWidth];

                for (int r = 0; r < NextHeight; r++)
                {
                    for (int c = 0; c < NextWidth; c++)
                    {
                        NextPattern[r, c] = 1;
                    }
                }
            }

            public void OnBoardTick(int[,] board, Block currentBlock, ref bool isGameOver)
            {
                _elapsedTime += 0.05;
                if (_elapsedTime >= _spawnInterval)
                {
                    _elapsedTime = 0;
                    RiseFromBottom(board, currentBlock, ref isGameOver);
                    PrepareNextWarning(board.GetLength(1));
                }
            }

            public void RiseFromBottom(int[,] board, Block currentBlock, ref bool isGameOver)
            {
                int boardHeight = board.GetLength(0);

                // 1. 차지할 가로 영역(NextStartX ~ NextStartX + NextWidth - 1)의 기존 블록을 NextHeight만큼 위로 밀어냄
                for (int c = NextStartX; c < NextStartX + NextWidth; c++)
                {
                    for (int r = 0; r < NextHeight; r++)
                    {
                        if (board[r, c] != 0)
                        {
                            isGameOver = true;
                        }
                    }

                    for (int r = 0; r < boardHeight - NextHeight; r++)
                    {
                        board[r, c] = board[r + NextHeight, c];
                    }

                    // 2. 맨 밑바닥 공간을 솟아오른 블록(코드 8: 회색 장애물 블록)으로 채움
                    for (int r = 0; r < NextHeight; r++)
                    {
                        int targetRow = boardHeight - NextHeight + r;
                        board[targetRow, c] = NextPattern[r, c] == 1 ? 8 : 0;
                    }
                }

                // 3. 현재 낙하 중인 블록과 겹치지 않도록 위로 보정
                while (currentBlock != null && !currentBlock.CanMove(board, 0, 0))
                {
                    if (currentBlock.Y > 0)
                    {
                        currentBlock.Y--;
                    }
                    else
                    {
                        isGameOver = true;
                        break;
                    }
                }
            }

            public void RenderPreview(int[,] board, ConsoleColor[,] previewColors)
            {
                int boardHeight = board.GetLength(0);
                for (int r = 0; r < NextHeight; r++)
                {
                    for (int c = 0; c < NextWidth; c++)
                    {
                        if (NextPattern[r, c] == 1)
                        {
                            int targetRow = boardHeight - NextHeight + r;
                            int targetCol = NextStartX + c;
                            if (targetRow >= 0 && targetRow < boardHeight)
                            {
                                previewColors[targetRow, targetCol] = ConsoleColor.DarkRed;
                            }
                        }
                    }
                }
            }

            public string GetWarningInfo()
            {
                double remain = Math.Max(0, _spawnInterval - _elapsedTime);
                return $"[바닥 솟아오름 경고] {remain:F1}초 후 X:{NextStartX + 1}~{NextStartX + NextWidth} 열에 {NextWidth}x{NextHeight} 블록 생성";
            }
        }

        public class TetrisGame
        {
            private const int Width = 10;
            private const int Height = 20;
            private readonly int[,] _board = new int[Height, Width];
            private readonly Random _random = new Random();

            private Block _currentBlock;
            private readonly UpFloor _upFloorObstacle;
            private int _score;
            private bool _isGameOver;
            private double _gravityTimer;
            private const double GravityInterval = 0.5;

            public TetrisGame()
            {
                _upFloorObstacle = new UpFloor(8.0);
                SpawnBlock();
            }

            private void SpawnBlock()
            {
                int pick = _random.Next(5);
                switch (pick)
                {
                    case 0: _currentBlock = new IBlock(); break;
                    case 1: _currentBlock = new OBlock(); break;
                    case 2: _currentBlock = new TBlock(); break;
                    case 3: _currentBlock = new LBlock(); break;
                    default: _currentBlock = new TimerBlock(5.0); break;
                }

                if (!_currentBlock.CanMove(_board, 0, 0))
                {
                    _isGameOver = true;
                }
            }

            public void Run()
            {
                Console.CursorVisible = false;

                while (!_isGameOver)
                {
                    ProcessInput();
                    Update(0.05);
                    Render();
                    Thread.Sleep(50);
                }

                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("==================================");
                Console.WriteLine("           GAME OVER!             ");
                Console.WriteLine($"        최종 점수: {_score}            ");
                Console.WriteLine("==================================");
                Console.ResetColor();
                Console.CursorVisible = true;
            }

            private void ProcessInput()
            {
                while (Console.KeyAvailable)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.LeftArrow:
                            _currentBlock.Move(_board, -1, 0);
                            break;
                        case ConsoleKey.RightArrow:
                            _currentBlock.Move(_board, 1, 0);
                            break;
                        case ConsoleKey.DownArrow:
                            if (!_currentBlock.Move(_board, 0, 1))
                            {
                                LockBlock();
                            }
                            break;
                        case ConsoleKey.UpArrow:
                            if (_currentBlock is IRollable rollable)
                            {
                                rollable.Rotate(_board);
                            }
                            break;
                        case ConsoleKey.Spacebar:
                            while (_currentBlock.Move(_board, 0, 1)) { }
                            LockBlock();
                            break;
                    }
                }
            }

            private void Update(double deltaTime)
            {
                bool isVanished = false;
                _currentBlock.OnUpdate(deltaTime, ref isVanished);

                if (isVanished)
                {
                    SpawnBlock();
                    return;
                }

                // UpFloor 다형성 장애물 갱신
                IBoardObstacle obstacle = _upFloorObstacle;
                obstacle.OnBoardTick(_board, _currentBlock, ref _isGameOver);
                if (_isGameOver) return;

                _gravityTimer += deltaTime;
                if (_gravityTimer >= GravityInterval)
                {
                    _gravityTimer = 0;
                    if (_currentBlock.CanFall(_board))
                    {
                        _currentBlock.Move(_board, 0, 1);
                    }
                    else
                    {
                        LockBlock();
                    }
                }
            }

            private void LockBlock()
            {
                int rows = _currentBlock.Matrix.GetLength(0);
                int cols = _currentBlock.Matrix.GetLength(1);
                int colorCode = GetColorCode(_currentBlock.Color);

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (_currentBlock.Matrix[r, c] != 0)
                        {
                            int boardY = _currentBlock.Y + r;
                            int boardX = _currentBlock.X + c;
                            if (boardY >= 0 && boardY < Height && boardX >= 0 && boardX < Width)
                            {
                                _board[boardY, boardX] = colorCode;
                            }
                        }
                    }
                }

                ClearFullLines();
                SpawnBlock();
            }

            private void ClearFullLines()
            {
                int linesCleared = 0;

                for (int r = Height - 1; r >= 0; r--)
                {
                    bool isFull = true;
                    for (int c = 0; c < Width; c++)
                    {
                        if (_board[r, c] == 0)
                        {
                            isFull = false;
                            break;
                        }
                    }

                    if (isFull)
                    {
                        linesCleared++;
                        for (int moveRow = r; moveRow > 0; moveRow--)
                        {
                            for (int c = 0; c < Width; c++)
                            {
                                _board[moveRow, c] = _board[moveRow - 1, c];
                            }
                        }
                        for (int c = 0; c < Width; c++)
                        {
                            _board[0, c] = 0;
                        }
                        r++;
                    }
                }

                if (linesCleared > 0)
                {
                    _score += linesCleared * 100;
                }
            }

            private void Render()
            {
                Console.SetCursorPosition(0, 0);

                ConsoleColor[,] displayColors = new ConsoleColor[Height, Width];
                string[,] displayGlyphs = new string[Height, Width];

                for (int r = 0; r < Height; r++)
                {
                    for (int c = 0; c < Width; c++)
                    {
                        if (_board[r, c] != 0)
                        {
                            displayColors[r, c] = GetConsoleColor(_board[r, c]);
                            displayGlyphs[r, c] = "■";
                        }
                        else
                        {
                            displayColors[r, c] = ConsoleColor.DarkGray;
                            displayGlyphs[r, c] = "·";
                        }
                    }
                }

                // UpFloor 예고 구역 시각화 (채워질 위치를 미리 표시)
                ConsoleColor[,] previewColors = new ConsoleColor[Height, Width];
                IBoardObstacle obstacle = _upFloorObstacle;
                obstacle.RenderPreview(_board, previewColors);

                for (int r = 0; r < Height; r++)
                {
                    for (int c = 0; c < Width; c++)
                    {
                        if (previewColors[r, c] == ConsoleColor.DarkRed)
                        {
                            if (_board[r, c] == 0)
                            {
                                displayColors[r, c] = ConsoleColor.DarkRed;
                                displayGlyphs[r, c] = "▒";
                            }
                            else
                            {
                                displayColors[r, c] = ConsoleColor.Red;
                            }
                        }
                    }
                }

                // 현재 조작 중인 블록 덮어쓰기
                int bRows = _currentBlock.Matrix.GetLength(0);
                int bCols = _currentBlock.Matrix.GetLength(1);
                for (int r = 0; r < bRows; r++)
                {
                    for (int c = 0; c < bCols; c++)
                    {
                        if (_currentBlock.Matrix[r, c] != 0)
                        {
                            int dy = _currentBlock.Y + r;
                            int dx = _currentBlock.X + c;
                            if (dy >= 0 && dy < Height && dx >= 0 && dx < Width)
                            {
                                displayColors[dy, dx] = _currentBlock.Color;
                                displayGlyphs[dy, dx] = "■";
                            }
                        }
                    }
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"점수: {_score}   |   {_currentBlock.GetStatusText().PadRight(32)}");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(_upFloorObstacle.GetWarningInfo().PadRight(65));
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("┌────────────────────┐");

                for (int r = 0; r < Height; r++)
                {
                    Console.Write("│");
                    for (int c = 0; c < Width; c++)
                    {
                        Console.ForegroundColor = displayColors[r, c];
                        Console.Write(displayGlyphs[r, c] + " ");
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("│");
                }

                Console.WriteLine("└────────────────────┘");
                Console.WriteLine("조작: ← → (이동), ↑ (회전), ↓ (소프트 드롭), Space (하드 드롭)");
                Console.WriteLine("안내: 붉은 '▒' 영역은 곧 바닥에서 솟아오를 UpFloor 예고 구역입니다.");
            }

            private int GetColorCode(ConsoleColor color)
            {
                switch (color)
                {
                    case ConsoleColor.Cyan: return 1;
                    case ConsoleColor.Yellow: return 2;
                    case ConsoleColor.Magenta: return 3;
                    case ConsoleColor.DarkYellow: return 4;
                    case ConsoleColor.Red: return 5;
                    case ConsoleColor.DarkGray: return 8;
                    default: return 7;
                }
            }

            private ConsoleColor GetConsoleColor(int code)
            {
                switch (code)
                {
                    case 1: return ConsoleColor.Cyan;
                    case 2: return ConsoleColor.Yellow;
                    case 3: return ConsoleColor.Magenta;
                    case 4: return ConsoleColor.DarkYellow;
                    case 5: return ConsoleColor.Red;
                    case 8: return ConsoleColor.DarkGray;
                    default: return ConsoleColor.White;
                }
            }
        }

        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            TetrisGame game = new TetrisGame();
            game.Run();
        }
    }
}
