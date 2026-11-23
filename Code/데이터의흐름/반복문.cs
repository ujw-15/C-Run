// 반복문
// for반복문
// while 반복문

for(int dan = 2; dan <= 9; dan++)
{
    Console.WriteLine($"\n==={dan}단===");
    for(int num = 1; num <= 9; num++)
    {
        Console.WriteLine($"{dan} X {num} = {dan * num}");
    }
}

// while
int hp = 0;
bool gamef = false;
while(true)
{
    if (hp <= 0) gamef = true;

    if(gamef)
    {
        break;
    }
}

