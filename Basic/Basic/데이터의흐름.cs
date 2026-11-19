using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// 가위 바위 보를 조건문으로 만들때 조건을 내가 만든다.

// 나: 가위 남: 바위 조건으로 표현해서 이긴사람은 점수를 1점 얻는다.
//          남(가위,바위,보)
// 3X3 코드 만들어서.
// 내가 가위 입력을하면 어떤 결과가 나오는지 점수판에 나오게 구현한다.

namespace Basic
{
    internal class 데이터의흐름
    {
        static void Main()
        {
            // if ,else if, if
            bool GameFinish = true;
            int score = 70;
            if(GameFinish)
            {
                if(60 <= score && 80 > score)
                {
                    Console.WriteLine("B");
                }
                else if (80 <= score && 100 > score)
                {
                    Console.WriteLine("A");
                }
                else
                {
                    Console.WriteLine("미정의");
                }
                    Console.WriteLine("종료");
            }

            string name = "파이리";

            // switch 스위치
            switch(name)
            {
                
            }

        }
    }
}
