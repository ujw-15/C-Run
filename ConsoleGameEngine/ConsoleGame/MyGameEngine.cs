namespace ConsoleGameEngineDemo
{
    class MyGameEngine : BaseGameEngine
    {
        private const int MapWidth = 20;
        private const int MapHeight = 12;

        // 1. 깜빡임 문제 해결을 위한 더블 버퍼(현재 프레임 vs 이전 프레임)
        private char[,] _currentBuffer = new char[MapHeight, MapWidth];
        private char[,] _prevBuffer = new char[MapHeight, MapWidth];
        private bool _isFirstRender = true;

        // 테스트용 플레이어 좌표 및 이동 방향
        private int _playerX = 5;
        private int _playerY = 5;
        private int _moveX = 0;
        private int _moveY = 0;
        private int _tickCount = 0;

        public MyGameEngine()
        {
            // 이전 버퍼 초기화 (첫 프레임에 모든 칸이 그려지도록 널 문자로 채움)
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    _prevBuffer[y, x] = '\0';
                }
            }
        }

        public override void Input()
        {
            _moveX = 0;
            _moveY = 0;

            // 키 입력 버퍼가 밀리지 않도록 쌓인 입력을 모두 소비
            while (Console.KeyAvailable)
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.A:
                        _moveX = -1;
                        break;
                    case ConsoleKey.RightArrow:
                    case ConsoleKey.D:
                        _moveX = 1;
                        break;
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        _moveY = -1;
                        break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        _moveY = 1;
                        break;
                    case ConsoleKey.Escape:
                        IsRunning = false;
                        break;
                }
            }
        }

        public override void Update()
        {
            _tickCount++;

            // 벽(테두리)을 넘지 않도록 좌표 갱신
            int nextX = _playerX + _moveX;
            int nextY = _playerY + _moveY;

            if (nextX >= 1 && nextX < MapWidth - 1)
                _playerX = nextX;

            if (nextY >= 1 && nextY < MapHeight - 1)
                _playerY = nextY;

            // 현재 프레임의 가상 화면(버퍼) 구성
            BuildFrameBuffer();
        }

        private void BuildFrameBuffer()
        {
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    // 외곽 벽 그리기
                    if (y == 0 || y == MapHeight - 1 || x == 0 || x == MapWidth - 1)
                    {
                        _currentBuffer[y, x] = '#';
                    }
                    else
                    {
                        _currentBuffer[y, x] = '.';
                    }
                }
            }

            // 플레이어 위치 배치
            _currentBuffer[_playerY, _playerX] = 'P';
        }

        public override void Rendering()
        {
            // [해결 1: 깜빡임 문제]
            // Console.Clear()를 쓰지 않고, 이전 프레임과 달라진 좌표만 찾아 SetCursorPosition으로 덮어씀
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    if (_currentBuffer[y, x] != _prevBuffer[y, x])
                    {
                        // [해결 2: 줄 밀리는 문제]
                        // 1) 2바이트 특수문자(■, □) 대신 1바이트 ASCII 문자(#, ., P)만 사용
                        // 2) 가로 간격을 2칸(x * 2)으로 고정해 가로/세로 비율과 줄 맞춤 유지
                        // 3) WriteLine 대신 정확한 좌표(SetCursorPosition)에 Write 호출
                        Console.SetCursorPosition(x * 2, y);
                        Console.Write(_currentBuffer[y, x]);
                        Console.Write(' ');

                        // 출력한 내용을 이전 버퍼에 동기화
                        _prevBuffer[y, x] = _currentBuffer[y, x];
                    }
                }
            }

            // [해결 2-보완: 텍스트 줄 밀림 및 잔상 문제]
            // 문자열 길이가 변해도 이전 글자가 남거나 다음 줄로 밀리지 않게 PadRight로 고정 폭 확보
            Console.SetCursorPosition(0, MapHeight + 1);
            string status = $"Pos: ({_playerX}, {_playerY}) | Frame: {_tickCount}";
            Console.Write(status.PadRight(MapWidth * 2));

            if (_isFirstRender)
            {
                Console.SetCursorPosition(0, MapHeight + 2);
                Console.Write("Move: Arrow Keys / WASD | Exit: ESC".PadRight(MapWidth * 2));
                _isFirstRender = false;
            }
        }
    }
}
