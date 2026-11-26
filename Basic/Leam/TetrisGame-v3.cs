using System;
using System.Collections.Generic;
using System.Threading;

namespace TetrisPolymorphism
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }
            catch { }

            TetrisGame game = new TetrisGame();
            game.Run();
        }

        // 회전 가능 여부를 나타내는 인터페이스
        public interface IRollable
        {
            void Rotate(int[,] board, int width, int height);
        }

        // 보드 환경에 영향을 주는 장애물/기믹 인터페이스 (다형성 확장)
        public interface IBoardObstacle
        {
            void UpdateObstacle(double deltaTime, int[,] board, int width, int height, ref Block currentBlock, ref bool isGameOver);
            void RenderPreview(ConsoleColor[,] previewColors, char[,] previewChars, int width, int height);
            string GetWarningInfo();
        }

        // 모든 테트리스 블록의 공통 요소와 기능을 추상화한 추상 클래스
        public abstract class Block
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int[,] Shape { get; protected set; }
            public ConsoleColor Color { get; protected set; }
            public bool IsDestroyed { get; protected set; }

            public Block(int startX, int startY, ConsoleColor color)
            {
                X = startX;
                Y = startY;
                Color = color;
                IsDestroyed = false;
            }

            // 다른 블록이나 벽과 겹치지 않는지 검사
            public bool CheckCollision(int nextX, int nextY, int[,] targetShape, int[,] board, int width, int height)
            {
                int rows = targetShape.GetLength(0);
                int cols = targetShape.GetLength(1);

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (targetShape[r, c] != 0)
                        {
                            int boardX = nextX + c;
                            int boardY = nextY + r;

                            // 좌우 및 바닥 경계 이탈
                            if (boardX < 0 || boardX >= width || boardY >= height)
                                return true;

                            // 보드 상단 위쪽은 허용하되 보드 내부에서 기존 블록과 충돌 시 true
                            if (boardY >= 0 && board[boardY, boardX] != 0)
                                return true;
                        }
                    }
                }
                return false;
            }

            // 좌우 및 아래 이동
            public bool Move(int dx, int dy, int[,] board, int width, int height)
            {
                if (!CheckCollision(X + dx, Y + dy, Shape, board, width, height))
                {
                    X += dx;
                    Y += dy;
                    return true;
                }
                return false;
            }

            // 아래로 더 떨어질 수 있는지 확인
            public bool CanFall(int[,] board, int width, int height)
            {
                return !CheckCollision(X, Y + 1, Shape, board, width, height);
            }

            // 다형성을 위한 가상 갱신 메서드 (기본 블록은 별도 특수 동작 없음)
            public virtual void OnUpdate(double deltaTime)
            {
            }

            // 현재 상태 정보 반환 (UI 표시용)
            public virtual string GetStatusText()
            {
                return "일반 블록";
            }

            // 시계 방향 90도 회전 행렬 계산 헬퍼
            protected int[,] GetRotatedMatrix()
            {
                int rows = Shape.GetLength(0);
                int cols = Shape.GetLength(1);
                int[,] rotated = new int[cols, rows];

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        rotated[c, rows - 1 - r] = Shape[r, c];
                    }
                }
                return rotated;
            }
        }

        // I 블록: Block 상속 + IRollable 구현
        public class IBlock : Block, IRollable
        {
            public IBlock(int startX, int startY) : base(startX, startY, ConsoleColor.Cyan)
            {
                Shape = new int[,]
                {
                    { 0, 0, 0, 0 },
                    { 1, 1, 1, 1 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 }
                };
            }

            public void Rotate(int[,] board, int width, int height)
            {
                int[,] rotated = GetRotatedMatrix();
                if (!CheckCollision(X, Y, rotated, board, width, height))
                {
                    Shape = rotated;
                }
            }
        }

        // T 블록: Block 상속 + IRollable 구현
        public class TBlock : Block, IRollable
        {
            public TBlock(int startX, int startY) : base(startX, startY, ConsoleColor.Magenta)
            {
                Shape = new int[,]
                {
                    { 0, 2, 0 },
                    { 2, 2, 2 },
                    { 0, 0, 0 }
                };
            }

            public void Rotate(int[,] board, int width, int height)
            {
                int[,] rotated = GetRotatedMatrix();
                if (!CheckCollision(X, Y, rotated, board, width, height))
                {
                    Shape = rotated;
                }
            }
        }

        // 정사각형(O) 블록: 회전할 필요가 없으므로 IRollable을 구현하지 않음
        public class OBlock : Block
        {
            public OBlock(int startX, int startY) : base(startX, startY, ConsoleColor.Yellow)
            {
                Shape = new int[,]
                {
                    { 3, 3 },
                    { 3, 3 }
                };
            }
        }

        // 특수 블록: 일정 시간이 지나면 사라지는 TimerBlock (다형성 활용)
        public class TimerBlock : Block, IRollable
        {
            public double DeathTimer { get; private set; }

            public TimerBlock(int startX, int startY, double seconds = 5.0) : base(startX, startY, ConsoleColor.Red)
            {
                DeathTimer = seconds;
                Shape = new int[,]
                {
                    { 4, 4, 0 },
                    { 0, 4, 4 },
                    { 0, 0, 0 }
                };
            }

            public override void OnUpdate(double deltaTime)
            {
                TimeDeath(deltaTime);
            }

            // 설정된 시간이 지나면 스스로 소멸
            public void TimeDeath(double deltaTime)
            {
                DeathTimer -= deltaTime;
                if (DeathTimer <= 0)
                {
                    DeathTimer = 0;
                    IsDestroyed = true;
                }
            }

            public override string GetStatusText()
            {
                return $"타이머 블록! 소멸까지 {DeathTimer:F1}초";
            }

            public void Rotate(int[,] board, int width, int height)
            {
                int[,] rotated = GetRotatedMatrix();
                if (!CheckCollision(X, Y, rotated, board, width, height))
                {
                    Shape = rotated;
                }
            }
        }

        // 새로운 규칙: 일정 시간마다 맨 밑바닥에서 최대 3x3 크기로 솟아오르며 기존 블록을 밀어내는 UpFloor
        public class UpFloor : Block, IBoardObstacle
        {
            private readonly Random _rand = new Random();
            private readonly int _boardWidth;

            public double RiseInterval { get; private set; }
            public double TimeRemaining { get; private set; }

            // 다음에 솟아오를 위치와 크기 (사전 예고용)
            public int NextStartX { get; private set; }
            public int NextWidth { get; private set; }
            public int NextHeight { get; private set; }

            public UpFloor(int boardWidth, double intervalSeconds = 8.0) : base(0, 0, ConsoleColor.DarkRed)
            {
                _boardWidth = boardWidth;
                RiseInterval = intervalSeconds;
                PlanNextRise();
            }

            // 다음 솟아오름의 위치와 크기(최대 3x3)를 미리 결정
            private void PlanNextRise()
            {
                NextWidth = _rand.Next(1, 4);  // 가로 1~3칸
                NextHeight = _rand.Next(1, 4); // 세로 1~3칸
                NextStartX = _rand.Next(0, _boardWidth - NextWidth + 1);
                TimeRemaining = RiseInterval;

                // Block 상속 속성에 현재 예고된 형태 동기화
                X = NextStartX;
                Shape = new int[NextHeight, NextWidth];
                for (int r = 0; r < NextHeight; r++)
                {
                    for (int c = 0; c < NextWidth; c++)
                    {
                        Shape[r, c] = 5; // UpFloor 블록 식별 코드 (5)
                    }
                }
            }

            public override void OnUpdate(double deltaTime)
            {
                TimeRemaining -= deltaTime;
            }

            public void UpdateObstacle(double deltaTime, int[,] board, int width, int height, ref Block currentBlock, ref bool isGameOver)
            {
                OnUpdate(deltaTime);

                if (TimeRemaining <= 0)
                {
                    RiseFromBottom(board, width, height, ref currentBlock, ref isGameOver);
                    PlanNextRise();
                }
            }

            // 맨 밑 칸으로부터 블록이 솟아오르며 기존 블록을 위로 밀어낸 후 공간을 차지
            public void RiseFromBottom(int[,] board, int width, int height, ref Block currentBlock, ref bool isGameOver)
            {
                for (int c = NextStartX; c < NextStartX + NextWidth; c++)
                {
                    // 최상단 영역에 기존 블록이 있어 밀려나면서 보드 밖으로 넘어가면 게임 오버
                    for (int r = 0; r < NextHeight; r++)
                    {
                        if (board[r, c] != 0)
                        {
                            isGameOver = true;
                        }
                    }

                    // 기존 블록들을 NextHeight만큼 위쪽으로 밀어냄
                    for (int r = 0; r < height - NextHeight; r++)
                    {
                        board[r, c] = board[r + NextHeight, c];
                    }

                    // 맨 밑바닥 공간을 솟아오른 블록(5)으로 채움
                    for (int r = height - NextHeight; r < height; r++)
                    {
                        board[r, c] = 5;
                    }
                }

                // 현재 낙하 중인 블록이 솟아오른 블록과 겹칠 경우 위로 보정
                if (currentBlock != null)
                {
                    while (currentBlock.CheckCollision(currentBlock.X, currentBlock.Y, currentBlock.Shape, board, width, height) && currentBlock.Y > 0)
                    {
                        currentBlock.Y--;
                    }
                }
            }

            // 어디 부분이 채워질지 보드 위에 미리 표시 (인덱스 범위 안전 검증 포함)
            public void RenderPreview(ConsoleColor[,] previewColors, char[,] previewChars, int width, int height)
            {
                int startR = Math.Max(0, height - NextHeight);
                int endC = Math.Min(width, NextStartX + NextWidth);

                for (int r = startR; r < height; r++)
                {
                    for (int c = NextStartX; c < endC; c++)
                    {
                        previewChars[r, c] = '▒';
                        previewColors[r, c] = ConsoleColor.Red;
                    }
                }
            }

            public string GetWarningInfo()
            {
                return $"[바닥 솟아오름 예고] {TimeRemaining:F1}초 후 | 위치: {NextStartX + 1}~{NextStartX + NextWidth}열 (크기 {NextWidth}x{NextHeight})";
            }
        }

        // 입력, 갱신, 렌더링을 총괄하는 테트리스 게임 엔진
        public class TetrisGame
        {
            private const int Width = 10;
            private const int Height = 20;

            private readonly int[,] _board = new int[Height, Width];
            private readonly Random _random = new Random();
            private readonly List<IBoardObstacle> _obstacles = new List<IBoardObstacle>();

            private Block _currentBlock;
            private bool _isGameOver = false;
            private int _score = 0;

            private double _fallTimer = 0;
            private readonly double _fallInterval = 0.5;

            private static readonly ConsoleColor[] BlockColors =
            {
                ConsoleColor.Black,
                ConsoleColor.Cyan,     // 1: IBlock
                ConsoleColor.Magenta,  // 2: TBlock
                ConsoleColor.Yellow,   // 3: OBlock
                ConsoleColor.Red,      // 4: TimerBlock
                ConsoleColor.DarkRed   // 5: UpFloor
            };

            public void Run()
            {
                _obstacles.Add(new UpFloor(Width, 8.0));
                SpawnBlock();
                DateTime lastTime = DateTime.Now;

                while (!_isGameOver)
                {
                    DateTime now = DateTime.Now;
                    double deltaTime = (now - lastTime).TotalSeconds;
                    lastTime = now;

                    ProcessInput();
                    Update(deltaTime);
                    Render();

                    Thread.Sleep(30);
                }

                Console.Clear();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("================================");
                Console.WriteLine("           GAME OVER!           ");
                Console.WriteLine($"        최종 점수: {_score}        ");
                Console.WriteLine("================================");
                Console.ResetColor();
            }

            // 1. 입력 처리 (Input)
            private void ProcessInput()
            {
                while (Console.KeyAvailable)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    if (_currentBlock == null) continue;

                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.LeftArrow:
                            _currentBlock.Move(-1, 0, _board, Width, Height);
                            break;
                        case ConsoleKey.RightArrow:
                            _currentBlock.Move(1, 0, _board, Width, Height);
                            break;
                        case ConsoleKey.DownArrow:
                            if (_currentBlock.Move(0, 1, _board, Width, Height))
                            {
                                _fallTimer = 0;
                            }
                            break;
                        case ConsoleKey.UpArrow:
                            // 다형성 및 인터페이스 타입 검사를 통해 회전 가능한 블록만 회전 실행
                            if (_currentBlock is IRollable rollableBlock)
                            {
                                rollableBlock.Rotate(_board, Width, Height);
                            }
                            break;
                        case ConsoleKey.Spacebar:
                            while (_currentBlock.Move(0, 1, _board, Width, Height)) { }
                            LockBlock();
                            ClearFullLines();
                            SpawnBlock();
                            _fallTimer = 0;
                            break;
                    }
                }
            }

            // 2. 게임 상태 갱신 (Update)
            private void Update(double deltaTime)
            {
                // 등록된 모든 보드 장애물/기믹 갱신 (다형성)
                foreach (var obstacle in _obstacles)
                {
                    obstacle.UpdateObstacle(deltaTime, _board, Width, Height, ref _currentBlock, ref _isGameOver);
                }

                if (_isGameOver || _currentBlock == null) return;

                // 현재 블록의 고유 상태 갱신 (TimerBlock 등 다형성 호출)
                _currentBlock.OnUpdate(deltaTime);

                if (_currentBlock.IsDestroyed)
                {
                    SpawnBlock();
                    _fallTimer = 0;
                    return;
                }

                // 중력에 의한 자동 낙하
                _fallTimer += deltaTime;
                if (_fallTimer >= _fallInterval)
                {
                    _fallTimer = 0;
                    if (_currentBlock.CanFall(_board, Width, Height))
                    {
                        _currentBlock.Move(0, 1, _board, Width, Height);
                    }
                    else
                    {
                        LockBlock();
                        ClearFullLines();
                        SpawnBlock();
                    }
                }
            }

            // 3. 화면 출력 (Render)
            private void Render()
            {
                Console.SetCursorPosition(0, 0);

                ConsoleColor[,] displayColors = new ConsoleColor[Height, Width];
                char[,] displayChars = new char[Height, Width];

                // 1) 기본 보드 및 쌓인 블록 채우기
                for (int r = 0; r < Height; r++)
                {
                    for (int c = 0; c < Width; c++)
                    {
                        int cell = _board[r, c];
                        if (cell != 0)
                        {
                            displayChars[r, c] = '■';
                            displayColors[r, c] = cell < BlockColors.Length ? BlockColors[cell] : ConsoleColor.Gray;
                        }
                        else
                        {
                            displayChars[r, c] = '·';
                            displayColors[r, c] = ConsoleColor.DarkGray;
                        }
                    }
                }

                // 2) 바닥 솟아오름 사전 예고 위치 덧그리기 (빈 칸일 경우 예고 기호 표시)
                foreach (var obstacle in _obstacles)
                {
                    obstacle.RenderPreview(displayColors, displayChars, Width, Height);
                }

                // 3) 현재 떨어지고 있는 블록 그리기 (인덱스 범위 안전 검증 포함)
                if (_currentBlock != null)
                {
                    int rows = _currentBlock.Shape.GetLength(0);
                    int cols = _currentBlock.Shape.GetLength(1);
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            if (_currentBlock.Shape[r, c] != 0)
                            {
                                int bx = _currentBlock.X + c;
                                int by = _currentBlock.Y + r;
                                if (by >= 0 && by < Height && bx >= 0 && bx < Width)
                                {
                                    displayChars[by, bx] = '■';
                                    displayColors[by, bx] = _currentBlock.Color;
                                }
                            }
                        }
                    }
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"점수: {_score,-10} | 현재: {_currentBlock?.GetStatusText(),-28}");
                foreach (var obstacle in _obstacles)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{obstacle.GetWarningInfo(),-60}");
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("조작: ← → (이동), ↑ (회전), ↓ (소프트드롭), Space (하드드롭)");
                Console.WriteLine("┌────────────────────┐");

                for (int r = 0; r < Height; r++)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("│");
                    for (int c = 0; c < Width; c++)
                    {
                        Console.ForegroundColor = displayColors[r, c];
                        Console.Write(displayChars[r, c] + " ");
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("│");
                }

                Console.WriteLine("└────────────────────┘");
                Console.ResetColor();
            }

            // 새로운 블록 무작위 생성 (UpFloor는 보드 기믹으로 별도 동작)
            private void SpawnBlock()
            {
                int type = _random.Next(4);
                int startX = Width / 2 - 2;
                int startY = 0;

                switch (type)
                {
                    case 0:
                        _currentBlock = new IBlock(startX, startY);
                        break;
                    case 1:
                        _currentBlock = new TBlock(startX, startY);
                        break;
                    case 2:
                        _currentBlock = new OBlock(startX + 1, startY);
                        break;
                    case 3:
                        _currentBlock = new TimerBlock(startX, startY, 5.0);
                        break;
                }

                // 생성 직후 이미 다른 블록과 겹치면 게임 오버
                if (_currentBlock.CheckCollision(_currentBlock.X, _currentBlock.Y, _currentBlock.Shape, _board, Width, Height))
                {
                    _isGameOver = true;
                }
            }

            // 바닥이나 다른 블록에 닿은 블록을 보드에 고정
            private void LockBlock()
            {
                int rows = _currentBlock.Shape.GetLength(0);
                int cols = _currentBlock.Shape.GetLength(1);

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        int val = _currentBlock.Shape[r, c];
                        if (val != 0)
                        {
                            int bx = _currentBlock.X + c;
                            int by = _currentBlock.Y + r;
                            if (by >= 0 && by < Height && bx >= 0 && bx < Width)
                            {
                                _board[by, bx] = val;
                            }
                        }
                    }
                }
            }

            // 가로 한 줄이 모두 채워지면 삭제하고 위쪽 블록들을 아래로 내림
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
        }
    }
}
