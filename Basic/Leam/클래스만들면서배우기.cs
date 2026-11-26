// 클래스를 만들어야 한다.

// 추상 클래스. 공통적인 기능들을 모두 빼낸다.
// 테트리스블럭이 공통적으로 가져야 할 요소
// 회전, 떨어지게, 한줄이 채워지면 사라짐, 겹치지 않는다, 모양

// I is 블록 -> 상속
// 정사각형 -> 회전을 할 수 있다 -> 회전가능한 IRollable 인터페이스

블록 I = new 블록("I");

class 블록
{
    public string shape = "ㅁ";

    public 블록(string shape)
    {
        this.shape = shape;
    }
}

// 블록의 모든 성질을 가지면서 독특한 새 기능 추가

class TimerBlock : 블록
{
    public int deathhTimer = 3;

    public TimerBlock(string shape) : base(shape)
    {

    }

    public void TimeDeath() // 시간이 지났을 때 없어지는 기능을 영어로 바꿔서 ()
    {
        // 몇초뒤 블록이 사라질지
    }
}

class UPfloor : 블록
{
    public UPfloor(string shape) : base(shape)
    {

    }
    
    public void full() // 일정시간마다 맨 밑에 칸으로 부터 블록이 쏫아오름며 기존 블럭을 위쪽으로 밀어낸 후 공간을 차지함
    {
        // 얼만큼 공간을 차지할지 가로,세ㄹ 3X3이 최대로
    }
    // 어디 부분이 채워질지 미리 알려주는 기능도 추가 

// 입력 갱신 렌더링을 구현해서 만들어야하며 c#으로 만들어야한다 하나의 클래스 안에 모두 만들고ㅗ 이 파일을 다운 받을수 있게 만들어라
// 다형성 지켜서 만들어라 테트리스의 기본구조는 ㅁ두 갖춘채
