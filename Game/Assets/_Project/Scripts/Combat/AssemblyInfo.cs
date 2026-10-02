using System.Runtime.CompilerServices;

// 규칙 모듈의 내부 타입(위치·이동·교전·확률 판정)을 테스트가 직접 검증할 수 있게 연다.
// 대안인 "검증이 필요한 타입을 전부 public" 은 표시 계층에까지 내부 규칙을 노출해
// 한 방향 참조(View → Data → Combat)의 경계가 흐려지므로 쓰지 않는다.
[assembly: InternalsVisibleTo("TAoB.Combat.Tests")]
