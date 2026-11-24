Enemy e1 = new Enemy("스켈레톤");

Enemy e2 = new Enemy("좀비");

Enemy e3 = new Enemy("고블린");

Console.WriteLine($"몬스터가 몇마리 소환되었나요? {Enemy.Count} 마리");

// 클래스이름 . 을 붙여서 안에 있는 데이터를 쓸 수 있는 문법이다.
// Class 이름 = new Class(); 형태 없이 쓸 수 있는 문법

// static을 쓰면은 클래스 이름으로 바로 쓸 수 있다.

class Enemy
{
    public static int Count;

    public string Name;

    public Enemy(string name)
    {
        Name = name;
        Count++;
    }

    public static void PrintCount()
    {
        Console.WriteLine($"카운트 수 : {Count}");
    }
}