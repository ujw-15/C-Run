
using System;
using System.Collections.Generic;

namespace StarCraftGameLoop
{
    public enum MovementType
    {
        Ground,
        Air
    }

    public enum UnitAttribute
    {
        Biological,
        Mechanical,
        Both
    }

    // 1. 인터페이스 정의

    public interface IClockable
    {
        bool IsCloaked { get; }
        void ToggleCloak();
    }

    public interface IDetecter
    {
        void Detect(IEnumerable<Unit> allUnits);
    }

    public interface ITransportI
    {
        int Capacity { get; }
        List<Unit> LoadedUnits { get; }
        void Load(Unit target);
        void UnloadAll();
    }

    public interface Worker
    {
        void GatherResource();
        void BuildStructure(string buildingName);
    }

    public interface IBurrowable
    {
        bool IsBurrowed { get; }
        void ToggleBurrow();
    }

    // 2. 공통 유닛 추상 클래스

    public abstract class Unit
    {
        public string Name { get; protected set; }
        public int MaxHp { get; protected set; }
        public int CurrentHp { get; protected set; }

        public MovementType MoveType { get; protected set; }
        public UnitAttribute Attribute { get; protected set; }

        public int X { get; protected set; }
        public int Y { get; protected set; }

        protected string LastActionLog = "대기 중";

        protected Unit(
            string name,
            int maxHp,
            MovementType moveType,
            UnitAttribute attribute,
            int x = 0,
            int y = 0)
        {
            Name = name;
            MaxHp = maxHp;
            CurrentHp = maxHp;
            MoveType = moveType;
            Attribute = attribute;

            X = x;
            Y = y;
        }

        public virtual void Move(int dx, int dy)
        {
            if (this is IBurrowable burrowable
                && burrowable.IsBurrowed)
            {
                LastActionLog = "잠복 상태라 이동 불가!";
                return;
            }

            X += dx;
            Y += dy;

            string moveDesc = MoveType == MovementType.Ground
                ? "지상 이동"
                : "공중 비행";

            LastActionLog = $"({X}, {Y}) 위치로 {moveDesc}";
        }

        public virtual void TakeDamage(int damage)
        {
            CurrentHp = Math.Max(0, CurrentHp - damage);

            LastActionLog =
                $"{damage} 피해 입음! (남은 체력: {CurrentHp}/{MaxHp})";
        }

        public abstract void UpdateTrait();

        public virtual string GetRenderInfo()
        {
            string typeStr = MoveType == MovementType.Ground
                ? "지상"
                : "공중";

            string attrStr;

            // C# 7.3 호환 switch문
            switch (Attribute)
            {
                case UnitAttribute.Biological:
                    attrStr = "생체";
                    break;

                case UnitAttribute.Mechanical:
                    attrStr = "기계";
                    break;

                default:
                    attrStr = "생체/기계";
                    break;
            }

            return $"[{Name}] ({typeStr}/{attrStr}) " +
                   $"HP: {CurrentHp}/{MaxHp} | " +
                   $"위치: ({X},{Y}) | 상태: {LastActionLog}";
        }
    }

    // 3. 종족별 추상 클래스

    public abstract class TerranUnit : Unit
    {
        protected TerranUnit(
            string name,
            int maxHp,
            MovementType moveType,
            UnitAttribute attribute,
            int x = 0,
            int y = 0)
            : base(name, maxHp, moveType, attribute, x, y)
        {
        }

        public override void UpdateTrait()
        {
            // 테란은 자동 체력 재생 없음
        }
    }

    public abstract class ZergUnit : Unit
    {
        protected ZergUnit(
            string name,
            int maxHp,
            MovementType moveType,
            UnitAttribute attribute,
            int x = 0,
            int y = 0)
            : base(name, maxHp, moveType, attribute, x, y)
        {
        }

        public override void UpdateTrait()
        {
            if (CurrentHp > 0 && CurrentHp < MaxHp)
            {
                CurrentHp = Math.Min(MaxHp, CurrentHp + 2);
            }
        }
    }

    public abstract class ProtossUnit : Unit
    {
        public int MaxShield { get; protected set; }
        public int CurrentShield { get; protected set; }

        protected ProtossUnit(
            string name,
            int maxHp,
            int maxShield,
            MovementType moveType,
            UnitAttribute attribute,
            int x = 0,
            int y = 0)
            : base(name, maxHp, moveType, attribute, x, y)
        {
            MaxShield = maxShield;
            CurrentShield = maxShield;
        }

        public override void TakeDamage(int damage)
        {
            if (CurrentShield >= damage)
            {
                CurrentShield -= damage;

                LastActionLog =
                    $"쉴드로 {damage} 피해 흡수! " +
                    $"(남은 쉴드: {CurrentShield}/{MaxShield})";
            }
            else
            {
                int remaining = damage - CurrentShield;

                CurrentShield = 0;

                CurrentHp = Math.Max(
                    0, CurrentHp - remaining);

                LastActionLog =
                    $"쉴드 파괴 후 체력 {remaining} 피해! " +
                    $"HP: {CurrentHp}/{MaxHp}";
            }
        }

        public override void UpdateTrait()
        {
            if (CurrentHp > 0 && CurrentShield < MaxShield)
            {
                CurrentShield = Math.Min(
                    MaxShield, CurrentShield + 3);
            }
        }

        public override string GetRenderInfo()
        {
            string typeStr = MoveType == MovementType.Ground
                ? "지상"
                : "공중";

            string attrStr;

            switch (Attribute)
            {
                case UnitAttribute.Biological:
                    attrStr = "생체";
                    break;

                case UnitAttribute.Mechanical:
                    attrStr = "기계";
                    break;

                default:
                    attrStr = "생체/기계";
                    break;
            }

            return $"[{Name}] ({typeStr}/{attrStr}) " +
                   $"HP: {CurrentHp}/{MaxHp} | " +
                   $"Shield: {CurrentShield}/{MaxShield} | " +
                   $"위치: ({X},{Y}) | 상태: {LastActionLog}";
        }
    }

    // 4. 세부 유닛 클래스

    // SCV
    public class SCV : TerranUnit, Worker
    {
        public SCV(int x = 0, int y = 0)
            : base(
                "SCV",
                60,
                MovementType.Ground,
                UnitAttribute.Both,
                x, y)
        {
        }

        public void GatherResource()
        {
            LastActionLog = "미네랄 채취 중...";
        }

        public void BuildStructure(string buildingName)
        {
            LastActionLog = $"{buildingName} 건설 중...";
        }
    }

    // 레이스
    public class Wraith : TerranUnit, IClockable
    {
        public bool IsCloaked { get; private set; }

        public Wraith(int x = 0, int y = 0)
            : base(
                "레이스",
                120,
                MovementType.Air,
                UnitAttribute.Mechanical,
                x, y)
        {
        }

        public void ToggleCloak()
        {
            IsCloaked = !IsCloaked;

            LastActionLog = IsCloaked
                ? "클로킹 활성화 (은신)"
                : "클로킹 해제";
        }
    }

    // 드랍쉽
    public class Dropship : TerranUnit, ITransportI
    {
        public int Capacity => 8;

        public List<Unit> LoadedUnits { get; }
            = new List<Unit>();

        public Dropship(int x = 0, int y = 0)
            : base(
                "드랍쉽",
                150,
                MovementType.Air,
                UnitAttribute.Mechanical,
                x, y)
        {
        }

        public void Load(Unit target)
        {
            if (target.MoveType != MovementType.Ground)
            {
                LastActionLog =
                    $"탑승 실패: {target.Name}은(는) 공중 유닛입니다.";

                return;
            }

            if (LoadedUnits.Count >= Capacity)
            {
                LastActionLog = "탑승 실패: 수송선 정원 초과";
                return;
            }

            LoadedUnits.Add(target);

            LastActionLog =
                $"{target.Name} 탑승 완료 " +
                $"({LoadedUnits.Count}/{Capacity})";
        }

        public void UnloadAll()
        {
            int count = LoadedUnits.Count;

            LoadedUnits.Clear();

            LastActionLog = count > 0
                ? $"유닛 {count}기 전원 하차 완료"
                : "하차할 유닛이 없습니다.";
        }
    }

    // 저글링
    public class Zergling : ZergUnit, IBurrowable
    {
        public bool IsBurrowed { get; private set; }

        public Zergling(int x = 0, int y = 0)
            : base(
                "저글링",
                35,
                MovementType.Ground,
                UnitAttribute.Biological,
                x, y)
        {
        }

        public void ToggleBurrow()
        {
            IsBurrowed = !IsBurrowed;

            LastActionLog = IsBurrowed
                ? "땅속으로 잠복(Burrow)"
                : "잠복 해제(Unburrow)";
        }
    }

    // 옵저버
    public class Observer : ProtossUnit, IClockable, IDetecter
    {
        public bool IsCloaked { get; private set; } = true;

        public Observer(int x = 0, int y = 0)
            : base(
                "옵저버",
                40,
                20,
                MovementType.Air,
                UnitAttribute.Mechanical,
                x, y)
        {
        }

        public void ToggleCloak()
        {
            LastActionLog = "옵저버는 상시 영구 은신 상태입니다.";
        }

        public void Detect(IEnumerable<Unit> allUnits)
        {
            List<string> detected = new List<string>();

            foreach (Unit u in allUnits)
            {
                if (u == this)
                    continue;

                if ((u is IClockable c && c.IsCloaked)
                    || (u is IBurrowable b && b.IsBurrowed))
                {
                    detected.Add(u.Name);
                }
            }

            LastActionLog = detected.Count > 0
                ? $"은신/잠복 유닛 탐지 성공: {string.Join(", ", detected)}"
                : "주변에 탐지된 은신/잠복 유닛 없음";
        }
    }

    // 5. 게임 엔진

    public class GameEngine
    {
        private List<Unit> units = new List<Unit>();

        private int selectedIndex = 0;
        private int turnCount = 1;

        private bool isRunning = true;

        public void Init()
        {
            units.Add(new SCV(1, 1));
            units.Add(new Wraith(2, 3));
            units.Add(new Dropship(0, 5));
            units.Add(new Zergling(4, 2));
            units.Add(new Observer(3, 3));
        }

        public void Run()
        {
            Init();

            while (isRunning)
            {
                Render();

                string cmd = Input();

                Update(cmd);
            }
        }

        // 입력
        private string Input()
        {
            Console.Write("\n명령 입력 > ");

            return Console.ReadLine()?.Trim().ToLower() ?? "";
        }

        // 갱신
        private void Update(string cmd)
        {
            if (cmd == "q")
            {
                isRunning = false;
                return;
            }

            Unit currentUnit = units[selectedIndex];

            switch (cmd)
            {
                // 유닛 선택
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":

                    int idx = int.Parse(cmd) - 1;

                    if (idx >= 0 && idx < units.Count)
                    {
                        selectedIndex = idx;
                    }

                    return;

                // 이동
                case "m":

                    currentUnit.Move(1, 1);

                    break;

                // 피해
                case "h":

                    currentUnit.TakeDamage(15);

                    break;

                // 특수 기능
                case "s":

                    if (currentUnit is Worker worker)
                    {
                        worker.GatherResource();
                    }
                    else if (currentUnit is IClockable cloakable)
                    {
                        cloakable.ToggleCloak();
                    }
                    else if (currentUnit is IBurrowable burrowable)
                    {
                        burrowable.ToggleBurrow();
                    }
                    else if (currentUnit is ITransportI transport)
                    {
                        if (transport.LoadedUnits.Count == 0)
                        {
                            Unit groundUnit = units.Find(
                                u => u.MoveType == MovementType.Ground);

                            if (groundUnit != null)
                            {
                                transport.Load(groundUnit);
                            }
                        }
                        else
                        {
                            transport.UnloadAll();
                        }
                    }

                    break;

                // 탐지
                case "d":

                    foreach (Unit u in units)
                    {
                        if (u is IDetecter detecter)
                        {
                            detecter.Detect(units);
                        }
                    }

                    break;

                // 다음 턴
                case "n":

                    break;

                default:

                    Console.WriteLine(
                        "잘못된 입력입니다. 엔터를 눌러 계속하세요...");

                    Console.ReadLine();

                    return;
            }

            // 종족별 패시브 갱신
            foreach (Unit unit in units)
            {
                unit.UpdateTrait();
            }

            turnCount++;
        }

        // 렌더링
        private void Render()
        {
            Console.Clear();

            Console.WriteLine(
                "====================================================================================");

            Console.WriteLine(
                $" [StarCraft OOP 시뮬레이터] - Turn: {turnCount}");

            Console.WriteLine(
                "====================================================================================");

            for (int i = 0; i < units.Count; i++)
            {
                string selector = i == selectedIndex
                    ? "▶ "
                    : "  ";

                Console.WriteLine(
                    $"{selector}[{i + 1}] {units[i].GetRenderInfo()}");
            }

            Console.WriteLine(
                "------------------------------------------------------------------------------------");

            Console.WriteLine(
                "1~5 : 유닛 선택");

            Console.WriteLine(
                "M   : 이동");

            Console.WriteLine(
                "H   : 피격 (-15)");

            Console.WriteLine(
                "S   : 고유 기능 (은신/잠복/채취/수송)");

            Console.WriteLine(
                "D   : 은신 및 잠복 유닛 탐지");

            Console.WriteLine(
                "N   : 다음 턴");

            Console.WriteLine(
                "Q   : 게임 종료");

            Console.WriteLine(
                "====================================================================================");
        }
    }

    // 6. 프로그램 실행

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            GameEngine engine = new GameEngine();

            engine.Run();
        }
    }
}
