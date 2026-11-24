using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

// 판정 종류
enum JudgeType
{
    Perfect,
    Good,
    Miss
}

// 노트 클래스
class Note
{
    public int Lane;
    public long HitTime;
    public bool IsJudged;

    public Note(int lane, long hitTime)
    {
        Lane = lane;
        HitTime = hitTime;
        IsJudged = false;
    }
}

// 채보 클래스
class Chart
{
    public List<Note> Notes = new List<Note>();

    public Chart()
    {
        Notes.Add(new Note(0, 2000));
        Notes.Add(new Note(1, 2500));
        Notes.Add(new Note(2, 3000));
        Notes.Add(new Note(3, 3500));

        Notes.Add(new Note(0, 4000));
        Notes.Add(new Note(3, 4000));

        Notes.Add(new Note(1, 4500));
        Notes.Add(new Note(2, 5000));

        Notes.Add(new Note(0, 5250));
        Notes.Add(new Note(1, 5500));
        Notes.Add(new Note(2, 5750));
        Notes.Add(new Note(3, 6000));

        Notes.Add(new Note(0, 6500));
        Notes.Add(new Note(3, 6500));

        Notes.Add(new Note(1, 7000));
        Notes.Add(new Note(2, 7500));

        Notes.Add(new Note(0, 8000));
        Notes.Add(new Note(1, 8000));
        Notes.Add(new Note(2, 8000));
        Notes.Add(new Note(3, 8000));
    }
}

// 점수 관리 클래스
class ScoreManager
{
    public int Score = 0;
    public int Combo = 0;
    public int MaxCombo = 0;

    public int PerfectCount = 0;
    public int GoodCount = 0;
    public int MissCount = 0;

    public string Message = "";

    public void AddScore(JudgeType judge)
    {
        switch (judge)
        {
            case JudgeType.Perfect:
                Score += 100;
                Combo++;
                PerfectCount++;
                Message = "PERFECT";
                break;

            case JudgeType.Good:
                Score += 50;
                Combo++;
                GoodCount++;
                Message = "GOOD";
                break;

            case JudgeType.Miss:
                Combo = 0;
                MissCount++;
                Message = "MISS";
                break;
        }

        if (Combo > MaxCombo)
        {
            MaxCombo = Combo;
        }
    }
}



// 리듬게임 클래스
class RhythmGame
{
    Chart chart = new Chart();
    ScoreManager scoreManager = new ScoreManager();
    Stopwatch timer = new Stopwatch();

    const int LaneCount = 4;
    const int Height = 15;
    const int FallTime = 2000;

    bool isRunning = true;

    // 게임 실행
    public void Run()
    {
        Console.Clear();

        Console.WriteLine("===== RHYTHM GAME =====");
        Console.WriteLine("KEY : D F J K");
        Console.WriteLine("R : RESTART");
        Console.WriteLine("ESC : EXIT");
        Console.WriteLine("Press any key to start");

        Console.ReadKey(true);

        bool isPlaying = true;

        while (isPlaying)
        {
            Console.Clear();
            Console.CursorVisible = false;

            replay();

            // 게임 루프
            while (isRunning)
            {
                Input();
                Update();
                Render();

                Thread.Sleep(16);
            }

            timer.Stop();

            Result();

            ConsoleKey key = Console.ReadKey(true).Key;

            if (key != ConsoleKey.R)
            {
                isPlaying = false;
            }
        }

        Console.CursorVisible = true;
    }

    void replay()
    {
        chart = new Chart();
        scoreManager = new ScoreManager();

        isRunning = true;

        timer.Restart();
    }

    // 입력 처리
    void Input()
    {
        while (Console.KeyAvailable)
        {
            ConsoleKey key = Console.ReadKey(true).Key;

            switch (key)
            {
                case ConsoleKey.D:
                    HitNote(0);
                    break;

                case ConsoleKey.F:
                    HitNote(1);
                    break;

                case ConsoleKey.J:
                    HitNote(2);
                    break;

                case ConsoleKey.K:
                    HitNote(3);
                    break;

                case ConsoleKey.R:
                    replay();
                    return;

                case ConsoleKey.Escape:
                    isRunning = false;
                    return;
            }
        }
    }

    // 게임 상태 업데이트
    void Update()
    {
        long currentTime = timer.ElapsedMilliseconds;

        bool allJudged = true;

        foreach (Note note in chart.Notes)
        {
            if (note.IsJudged)
                continue;

            // 판정 시간을 놓친 경우
            if (currentTime > note.HitTime + 150)
            {
                note.IsJudged = true;

                scoreManager.AddScore(JudgeType.Miss);
            }

            if (!note.IsJudged)
            {
                allJudged = false;
            }
        }

        // 모든 노트의 판정이 끝나면 종료
        if (allJudged)
        {
            isRunning = false;
        }
    }

    // 노트 판정
    void HitNote(int lane)
    {
        long currentTime = timer.ElapsedMilliseconds;

        Note target = null;

        long minDifference = long.MaxValue;

        foreach (Note note in chart.Notes)
        {
            if (note.IsJudged || note.Lane != lane)
                continue;

            long difference =
                Math.Abs(note.HitTime - currentTime);

            if (difference < minDifference)
            {
                minDifference = difference;
                target = note;
            }
        }

        // 판정할 노트가 없는 경우
        if (target == null || minDifference > 150)
            return;

        target.IsJudged = true;

        // 시간 오차에 따른 판정
        if (minDifference <= 50)
        {
            scoreManager.AddScore(JudgeType.Perfect);
        }
        else
        {
            scoreManager.AddScore(JudgeType.Good);
        }
    }

    // 화면 출력
    void Render()
    {
        long currentTime = timer.ElapsedMilliseconds;

        string[,] board = new string[Height, LaneCount];

        // 화면 배열 초기화
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < LaneCount; x++)
            {
                board[y, x] = "   ";
            }
        }

        // 노트 위치 계산
        foreach (Note note in chart.Notes)
        {
            if (note.IsJudged)
                continue;

            long remainTime = note.HitTime - currentTime;

            if (remainTime > FallTime)
                continue;

            int y = (int)((1.0 -
                (double)remainTime / FallTime)
                * (Height - 1));

            y = Math.Min(y, Height - 1);

            if (y >= 0 && y < Height)
            {
                board[y, note.Lane] = "===";
            }
        }

        StringBuilder screen = new StringBuilder();

        screen.AppendLine("===== RHYTHM GAME =====");
        screen.AppendLine();

        screen.AppendLine(
            $"SCORE : {scoreManager.Score,-10}");

        screen.AppendLine(
            $"COMBO : {scoreManager.Combo,-10}");

        screen.AppendLine(
            $"MAX COMBO : {scoreManager.MaxCombo,-10}");

        screen.AppendLine();

        screen.AppendLine("+---+---+---+---+");

        // 2차원 배열 화면 출력
        for (int y = 0; y < Height; y++)
        {
            screen.Append("|");

            for (int x = 0; x < LaneCount; x++)
            {
                screen.Append(board[y, x] + "|");
            }

            screen.AppendLine();
        }

        // 판정선
        screen.AppendLine("+===+===+===+===+");

        // 조작키
        screen.AppendLine("| D | F | J | K |");
        screen.AppendLine("+---+---+---+---+");

        screen.AppendLine();

        screen.AppendLine(
            $"JUDGE : {scoreManager.Message,-10}");

        screen.AppendLine("ESC : EXIT");

        // 화면 깜빡임 방지
        Console.SetCursorPosition(0, 0);
        Console.Write(screen.ToString());
    }

    // 결과 화면
    void Result()
    {
        Console.Clear();

        Console.WriteLine("===== GAME RESULT =====");

        Console.WriteLine();

        Console.WriteLine($"SCORE     : {scoreManager.Score}");
        Console.WriteLine($"MAX COMBO : {scoreManager.MaxCombo}");

        Console.WriteLine();

        Console.WriteLine($"PERFECT : {scoreManager.PerfectCount}");
        Console.WriteLine($"GOOD    : {scoreManager.GoodCount}");
        Console.WriteLine($"MISS    : {scoreManager.MissCount}");

        Console.WriteLine();
        Console.WriteLine("=======================");
        Console.WriteLine("R : RESTART");
        Console.WriteLine("Other Key : EXIT");

        Console.CursorVisible = true;
    }
}

// 메인 클래스
class Program
{
    static void Main(string[] args)
    {
        RhythmGame game = new RhythmGame();

        game.Run();
    }
}