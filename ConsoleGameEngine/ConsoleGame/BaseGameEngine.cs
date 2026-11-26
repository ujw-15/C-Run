namespace ConsoleGameEngineDemo
{
    // abstract 추상 : 기능을 구현하진 않을거지만 너가 이 내용을 구현해야한다. - '약속'
    abstract class BaseGameEngine
    {
        protected bool IsRunning = true;
        protected int FrameDelayMs = 16; // 약 60 FPS

        // input 입력
        abstract public void Input();

        // Update 갱신
        abstract public void Update();

        // Rendering 렌더링
        abstract public void Rendering();

        // 공통 게임 루프
        public void Run()
        {
            Console.CursorVisible = false; // 커서 깜빡임 숨김
            Console.Clear();

            while (IsRunning)
            {
                Input();
                Update();
                Rendering();
                Thread.Sleep(FrameDelayMs);
            }

            Console.CursorVisible = true;
        }
    }
}
