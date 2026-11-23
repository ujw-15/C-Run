// 2020-11-18
// C# 프로그래밍 입문
// 학습 목표 : 화면에 내가 원하는 글자를 출력하고 싶다.

// 컴퓨터 : 작성한 글자를 어떻게 읽어?
// 입력과 출력
// 내가 어떤 크기에 어떤 물건을 넣어야 할지 딱 맞는 상자를 알려줄게 -> type 형태

// 변수
// 정수    int
// 실수    float
// 문자열  string
// 불      bool

// [데이터 + 로직] =>
// 변수
// 로직 : 어떻게 사용을 해야 진짜 게임이 끝난 것처럼 표현이 될까?

// 두번째 학습 목표 : 변수를 변형해서 원하는 흐름을 얻기

// 몬스터hp
// 주인공hp
// 몬스터atk
// 주인공atk
// '전투' : 슬라임의 턴. 슬라임이 용사의 체력을 슬라임의 공격력으로 공격

// 조건문 활용해서 간단한 승리 조건 구현

namespace Basic
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("이름을 입력하시오");

           // int hp = 100;
           // hp = hp - 100;
           // float version = 1.01f;
           // bool gameClear = false;
           //
           // Console.WriteLine(hp);
           // Console.WriteLine(version);
           // Console.WriteLine(gameClear);


            string name  = Console.ReadLine();
            Console.WriteLine(name);

            int slimeHP = 100;

            int PlayerHP = 100;

            int s_atk = 10;

            int p_atk = 20;

            Console.WriteLine("[슬라임 턴]");
            PlayerHP = PlayerHP - s_atk;
            Console.WriteLine($"용사의 체력은 : {PlayerHP}다");
            // 슬라임이 용사의 체력을 슬라임의 공격력으로 공격

            // 특정 경우에만 실행할 수 있게 해줄것인가

            // 조건과 반복

            if(slimeHP <= 0)
            {
                Console.WriteLine("승리");
            }
        }
    }
}
