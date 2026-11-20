
// 아이템 종류 열거형


// 갯수 9개
// 콘솔창 출력 3X3 반복문으로 
// | -아이템- | -아이템- | -아이템- |

// '조건,반복,배열,열거'

// 아이템 x개 .배열선언

// 반복적 코드

// 입력 - 업데이트 - 렌더링 방식

// 숫자는 1 ~ 9 클릭시 아이템 구매
// 상점나가기(종료) ESC키


using System;

namespace GameData
{
    // 아이템 종류 열거형
    enum ItemType
    {
        Sword,
        Shield,
        Potion,
        Bow,
        Helmet,
        Armor,
        Ring,
        Boots,
        Staff
    }

    class Program
    {
        // 아이템 9개 배열
        static ItemType[] items =
        {
            ItemType.Sword,
            ItemType.Shield,
            ItemType.Potion,
            ItemType.Bow,
            ItemType.Helmet,
            ItemType.Armor,
            ItemType.Ring,
            ItemType.Boots,
            ItemType.Staff
        };

        static bool isRunning = true;
        static string message = "구매할 아이템을 선택하세요.";
        static ConsoleKey inputKey;

        static void Main(string[] args)
        {
            while (isRunning)
            {
                Render();
                Input();
                Update();
            }

            Console.WriteLine("상점을 종료합니다.");
        }

        // 입력
        static void Input()
        {
            inputKey = Console.ReadKey(true).Key;
        }

        // 업데이트
        static void Update()
        {
            if (inputKey == ConsoleKey.Escape)
            {
                isRunning = false;
                return;
            }

            int number = (int)inputKey - (int)ConsoleKey.D1; // (int)ConsoleKey.D1

            if (number >= 0 && number < items.Length)
            {
                message = $"{items[number]} 구매 완료!";
            }
            else
            {
                message = "1~9 숫자를 입력해주세요.";
            }
        }

        // 렌더링
        static void Render()
        {
            Console.Clear();

            Console.WriteLine("======== 아이템 상점 ========");
            Console.WriteLine();

            for (int i = 0; i < items.Length; i++)
            {
                Console.Write($"| {i + 1}. {items[i],-8} "); // | 1. (0)item |

                if (i % 3 != 2)
                {
                    Console.WriteLine("\n");
                }
            }

            Console.WriteLine();
            Console.WriteLine(message);
            Console.WriteLine();
            Console.WriteLine("1~9 : 구매 / ESC : 종료");
            Console.Write("dkssud");
            

        }
    }
}
