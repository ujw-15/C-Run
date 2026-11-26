using System;
using System.Collections.Generic;

namespace ConsoleGameEngineDemo
{
    // 1980 Rogue 스타일의 몬스터 엔티티 클래스
    class Monster
    {
        public string Name;
        public char Symbol;
        public int X, Y;
        public int Hp;
        public int Attack;
        public int ExpReward;
    }

    // 1980 Rogue 스타일의 아이템 엔티티 클래스
    class Item
    {
        public string Name;
        public char Symbol; // '*': Gold, '!': Potion, ')': Weapon
        public int X, Y;
        public int Value;
    }

    // BaseGameEngine을 상속받은 1980 Rogue 게임 엔진
    class RogueGameEngine : BaseGameEngine
    {
        private const int MapWidth = 36;
        private const int MapHeight = 18;

        // 1. 깜빡임 및 줄 밀림 방지를 위한 더블 버퍼링
        private char[,] _currentBuffer = new char[MapHeight, MapWidth];
        private char[,] _prevBuffer = new char[MapHeight, MapWidth];
        private ConsoleColor[,] _currentColor = new ConsoleColor[MapHeight, MapWidth];
        private ConsoleColor[,] _prevColor = new ConsoleColor[MapHeight, MapWidth];

        // 1980 Rogue 맵 데이터 (' ': 공허, '#': 벽, '.': 방 바닥, '+': 복도, '>': 다음 층 계단)
        private char[,] _map = new char[MapHeight, MapWidth];
        private bool[,] _explored = new bool[MapHeight, MapWidth]; // 탐험한 지역 기억
        private bool[,] _visible = new bool[MapHeight, MapWidth];  // 현재 시야

        // 플레이어 스탯 (1980 로그 오리지널 스탯 구성)
        private int _playerX, _playerY;
        private int _level = 1;
        private int _dungeonFloor = 1;
        private int _hp = 15, _maxHp = 15;
        private int _str = 4; // 공격력
        private int _gold = 0;
        private int _exp = 0, _nextExp = 10;

        // 턴제 입력 및 상태 메시지
        private int _moveX = 0, _moveY = 0;
        private bool _turnTaken = false;
        private bool _isGameOver = false;
        private string _logMessage = "Welcome to the Dungeons of Doom! Find the stairs (>).";

        private List<Monster> _monsters = new List<Monster>();
        private List<Item> _items = new List<Item>();
        private Random _rand = new Random();

        public RogueGameEngine()
        {
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    _prevBuffer[y, x] = '\0';
                }
            }
            GenerateFloor();
        }

        // 절차적 던전 생성 (방 4~5개를 만들고 복도로 연결)
        private void GenerateFloor()
        {
            _monsters.Clear();
            _items.Clear();

            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    _map[y, x] = ' ';
                    _explored[y, x] = false;
                    _visible[y, x] = false;
                }
            }

            List<(int cx, int cy)> roomCenters = new List<(int, int)>();
            int roomAttempts = 12;

            for (int i = 0; i < roomAttempts; i++)
            {
                int w = _rand.Next(5, 9);
                int h = _rand.Next(4, 7);
                int rx = _rand.Next(1, MapWidth - w - 1);
                int ry = _rand.Next(1, MapHeight - h - 1);

                // 기존 방과 겹치는지 검사
                bool overlaps = false;
                for (int y = ry - 1; y <= ry + h; y++)
                {
                    for (int x = rx - 1; x <= rx + w; x++)
                    {
                        if (_map[y, x] != ' ') overlaps = true;
                    }
                }
                if (overlaps) continue;

                // 방 바닥('.')과 벽('#') 배치
                for (int y = ry; y < ry + h; y++)
                {
                    for (int x = rx; x < rx + w; x++)
                    {
                        if (y == ry || y == ry + h - 1 || x == rx || x == rx + w - 1)
                            _map[y, x] = '#';
                        else
                            _map[y, x] = '.';
                    }
                }

                int centerX = rx + w / 2;
                int centerY = ry + h / 2;

                // 이전 방과 복도('+')로 연결
                if (roomCenters.Count > 0)
                {
                    var prev = roomCenters[roomCenters.Count - 1];
                    CarveCorridor(prev.cx, prev.cy, centerX, centerY);
                }

                roomCenters.Add((centerX, centerY));
            }

            // 첫 번째 방에 플레이어 배치
            _playerX = roomCenters[0].cx;
            _playerY = roomCenters[0].cy;

            // 마지막 방에 다음 층 계단('>') 배치
            var lastRoom = roomCenters[roomCenters.Count - 1];
            _map[lastRoom.cy, lastRoom.cx] = '>';

            // 중간 방들에 몬스터와 아이템 배치
            for (int i = 1; i < roomCenters.Count; i++)
            {
                SpawnMonster(roomCenters[i].cx, roomCenters[i].cy);
                if (_rand.Next(100) < 70)
                {
                    SpawnItem(roomCenters[i].cx + 1, roomCenters[i].cy);
                }
            }

            UpdateFieldOfView();
            BuildFrameBuffer();
        }

        private void CarveCorridor(int x1, int y1, int x2, int y2)
        {
            int x = x1, y = y1;
            while (x != x2)
            {
                if (_map[y, x] == ' ' || _map[y, x] == '#') _map[y, x] = '+';
                x += (x < x2) ? 1 : -1;
            }
            while (y != y2)
            {
                if (_map[y, x] == ' ' || _map[y, x] == '#') _map[y, x] = '+';
                y += (y < y2) ? 1 : -1;
            }
        }

        private void SpawnMonster(int x, int y)
        {
            // 층수에 따라 1980 로그의 상징적인 몬스터 등장 (Bat, Hobgoblin, Orc)
            int roll = _rand.Next(100) + (_dungeonFloor * 10);
            if (roll < 50)
                _monsters.Add(new Monster { Name = "Bat", Symbol = 'B', X = x, Y = y, Hp = 5 + _dungeonFloor, Attack = 2, ExpReward = 4 });
            else if (roll < 85)
                _monsters.Add(new Monster { Name = "Hobgoblin", Symbol = 'H', X = x, Y = y, Hp = 8 + _dungeonFloor * 2, Attack = 3 + _dungeonFloor / 2, ExpReward = 7 });
            else
                _monsters.Add(new Monster { Name = "Orc", Symbol = 'O', X = x, Y = y, Hp = 12 + _dungeonFloor * 2, Attack = 4 + _dungeonFloor, ExpReward = 12 });
        }

        private void SpawnItem(int x, int y)
        {
            if (_map[y, x] != '.') return;

            int type = _rand.Next(3);
            if (type == 0)
                _items.Add(new Item { Name = "Gold", Symbol = '*', X = x, Y = y, Value = _rand.Next(10, 35) * _dungeonFloor });
            else if (type == 1)
                _items.Add(new Item { Name = "Healing Potion", Symbol = '!', X = x, Y = y, Value = 6 + _dungeonFloor * 2 });
            else
                _items.Add(new Item { Name = "Long Sword (+1)", Symbol = ')', X = x, Y = y, Value = 1 });
        }

        // 입력 처리 (턴제 방식이므로 키 입력 시 1턴 진행)
        public override void Input()
        {
            _moveX = 0;
            _moveY = 0;
            _turnTaken = false;

            while (Console.KeyAvailable)
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                if (_isGameOver)
                {
                    if (key == ConsoleKey.Escape || key == ConsoleKey.Enter) IsRunning = false;
                    return;
                }

                switch (key)
                {
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.A:
                        _moveX = -1; _turnTaken = true; break;
                    case ConsoleKey.RightArrow:
                    case ConsoleKey.D:
                        _moveX = 1; _turnTaken = true; break;
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        _moveY = -1; _turnTaken = true; break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        _moveY = 1; _turnTaken = true; break;
                    case ConsoleKey.Spacebar: // 제자리 휴식 (HP 자연 회복)
                        _turnTaken = true;
                        if (_hp < _maxHp && _rand.Next(100) < 40) _hp++;
                        _logMessage = "You rest for a moment...";
                        break;
                    case ConsoleKey.Escape:
                        IsRunning = false;
                        break;
                }
            }
        }

        // 상태 갱신 (플레이어 턴 -> 아이템/계단 판정 -> 몬스터 AI 턴 -> 시야 갱신)
        public override void Update()
        {
            if (!_turnTaken || _isGameOver) return;

            int nextX = _playerX + _moveX;
            int nextY = _playerY + _moveY;

            // 1. 이동하려는 칸에 몬스터가 있는지 확인 (범프 어택 전투)
            Monster target = _monsters.Find(m => m.X == nextX && m.Y == nextY);
            if (target != null)
            {
                int dmg = _rand.Next(1, _str + 2);
                target.Hp -= dmg;
                _logMessage = $"You hit the {target.Name} for {dmg} damage!";

                if (target.Hp <= 0)
                {
                    _logMessage = $"You defeated the {target.Name}! (+{target.ExpReward} EXP)";
                    _exp += target.ExpReward;
                    _monsters.Remove(target);
                    CheckLevelUp();
                }
            }
            // 2. 이동 가능한 지형인지 확인 ('.', '+', '>')
            else if (IsWalkable(nextX, nextY))
            {
                _playerX = nextX;
                _playerY = nextY;

                // 아이템 줍기 판정
                Item item = _items.Find(i => i.X == _playerX && i.Y == _playerY);
                if (item != null)
                {
                    if (item.Symbol == '*')
                    {
                        _gold += item.Value;
                        _logMessage = $"You found {item.Value} gold pieces!";
                    }
                    else if (item.Symbol == '!')
                    {
                        _hp = Math.Min(_maxHp, _hp + item.Value);
                        _logMessage = $"You drank a {item.Name}! HP restored.";
                    }
                    else if (item.Symbol == ')')
                    {
                        _str += item.Value;
                        _logMessage = $"You equipped a {item.Name}! Strength increased.";
                    }
                    _items.Remove(item);
                }

                // 다음 층 계단('>') 판정
                if (_map[_playerY, _playerX] == '>')
                {
                    _dungeonFloor++;
                    _logMessage = $"You descended to Dungeon Level {_dungeonFloor}!";
                    GenerateFloor();
                    return;
                }
            }

            // 3. 몬스터 턴 진행 (시야 내 몬스터가 플레이어 추적 및 공격)
            UpdateMonsters();

            // 4. 시야(Fog of War) 및 화면 버퍼 갱신
            UpdateFieldOfView();
            BuildFrameBuffer();
        }

        private bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
            char tile = _map[y, x];
            return tile == '.' || tile == '+' || tile == '>';
        }

        private void CheckLevelUp()
        {
            if (_exp >= _nextExp)
            {
                _level++;
                _exp -= _nextExp;
                _nextExp = (int)(_nextExp * 1.6);
                _maxHp += 5;
                _hp = _maxHp;
                _str += 1;
                _logMessage = $"Welcome to Level {_level}! Max HP and Strength increased!";
            }
        }

        private void UpdateMonsters()
        {
            foreach (var m in _monsters)
            {
                int dist = Math.Abs(m.X - _playerX) + Math.Abs(m.Y - _playerY);
                if (dist > 6) continue; // 일정 거리 밖이면 대기

                // 플레이어와 인접하면 공격
                if (dist == 1)
                {
                    int dmg = _rand.Next(1, m.Attack + 1);
                    _hp -= dmg;
                    _logMessage = $"The {m.Name} hits you for {dmg} damage!";

                    if (_hp <= 0)
                    {
                        _hp = 0;
                        _isGameOver = true;
                        _logMessage = $"YOU DIED on Floor {_dungeonFloor} with {_gold} Gold! (Press ENTER)";
                        break;
                    }
                }
                else
                {
                    // 플레이어 방향으로 1칸 추적
                    int dx = Math.Sign(_playerX - m.X);
                    int dy = Math.Sign(_playerY - m.Y);

                    int tryX = m.X + dx;
                    int tryY = m.Y;

                    if (IsWalkable(tryX, tryY) && !_monsters.Exists(other => other.X == tryX && other.Y == tryY))
                    {
                        m.X = tryX;
                    }
                    else
                    {
                        tryX = m.X;
                        tryY = m.Y + dy;
                        if (IsWalkable(tryX, tryY) && !_monsters.Exists(other => other.X == tryX && other.Y == tryY))
                        {
                            m.Y = tryY;
                        }
                    }
                }
            }
        }

        // 1980 로그 스타일 시야 계산 (주변 반경 4칸 밝힘 + 지나온 길 기억)
        private void UpdateFieldOfView()
        {
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    _visible[y, x] = false;
                }
            }

            int radius = 4;
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int vx = _playerX + dx;
                    int vy = _playerY + dy;
                    if (vx >= 0 && vx < MapWidth && vy >= 0 && vy < MapHeight)
                    {
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            _visible[vy, vx] = true;
                            _explored[vy, vx] = true;
                        }
                    }
                }
            }
        }

        private void BuildFrameBuffer()
        {
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    if (!_explored[y, x])
                    {
                        _currentBuffer[y, x] = ' ';
                        _currentColor[y, x] = ConsoleColor.Black;
                    }
                    else if (_visible[y, x])
                    {
                        _currentBuffer[y, x] = _map[y, x];
                        _currentColor[y, x] = (_map[y, x] == '>') ? ConsoleColor.Cyan :
                                             (_map[y, x] == '#') ? ConsoleColor.DarkYellow : ConsoleColor.Gray;
                    }
                    else
                    {
                        // 이미 탐험했지만 현재 시야 밖인 곳은 어둡게 표시 (전장의 안개)
                        _currentBuffer[y, x] = _map[y, x];
                        _currentColor[y, x] = ConsoleColor.DarkGray;
                    }
                }
            }

            // 시야 내 아이템 렌더링
            foreach (var item in _items)
            {
                if (_visible[item.Y, item.X])
                {
                    _currentBuffer[item.Y, item.X] = item.Symbol;
                    _currentColor[item.Y, item.X] = (item.Symbol == '*') ? ConsoleColor.Yellow :
                                                      (item.Symbol == '!') ? ConsoleColor.Magenta : ConsoleColor.Blue;
                }
            }

            // 시야 내 몬스터 렌더링
            foreach (var m in _monsters)
            {
                if (_visible[m.Y, m.X])
                {
                    _currentBuffer[m.Y, m.X] = m.Symbol;
                    _currentColor[m.Y, m.X] = ConsoleColor.Red;
                }
            }

            // 1980 로그의 상징: 플레이어 캐릭터 '@'
            _currentBuffer[_playerY, _playerX] = '@';
            _currentColor[_playerY, _playerX] = ConsoleColor.Green;
        }

        // 깜빡임 & 줄 밀림 없는 렌더링
        public override void Rendering()
        {
            // 상단 메시지 로그 (고정 폭 72칸으로 줄 밀림/잔상 방지)
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = _isGameOver ? ConsoleColor.Red : ConsoleColor.White;
            Console.Write(_logMessage.PadRight(MapWidth * 2));

            // 맵 영역 더블 버퍼 부분 갱신 (변경된 타일만 덮어쓰기)
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    if (_currentBuffer[y, x] != _prevBuffer[y, x] || _currentColor[y, x] != _prevColor[y, x])
                    {
                        Console.SetCursorPosition(x * 2, y + 1);
                        Console.ForegroundColor = _currentColor[y, x];
                        Console.Write(_currentBuffer[y, x]);
                        Console.Write(' ');

                        _prevBuffer[y, x] = _currentBuffer[y, x];
                        _prevColor[y, x] = _currentColor[y, x];
                    }
                }
            }

            // 하단 1980 로그 오리지널 상태 표시줄
            Console.SetCursorPosition(0, MapHeight + 1);
            Console.ForegroundColor = ConsoleColor.Yellow;
            string stats = $"Level:{_dungeonFloor}  Hits:{_hp}({_maxHp})  Str:{_str}  Gold:{_gold}  Exp:{_level}/{_exp}";
            Console.Write(stats.PadRight(MapWidth * 2));

            Console.SetCursorPosition(0, MapHeight + 2);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            string legend = "[@]Hero  [B/H/O]Monster  [*]Gold  [!]Potion  [)]Sword  [>]Stairs  [Space]Rest";
            Console.Write(legend.PadRight(MapWidth * 2));

            Console.ResetColor();
        }
    }
}
