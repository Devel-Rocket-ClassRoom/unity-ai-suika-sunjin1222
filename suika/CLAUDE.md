# CLAUDE.md — Suika Game Project

## 프로젝트 개요

Unity 2D (URP) 수이카 게임 클론. 과일을 떨어뜨려 같은 종류끼리 합쳐 수박을 만드는 물리 퍼즐.  
상세 기획은 [`Docs/GDD.md`](Docs/GDD.md) 참고 — 모든 구현 판단의 기준이 됨.

## 프로젝트 구조

```
Assets/
├── Scripts/
│   ├── FruitData.cs        ScriptableObject. 레벨·반지름·스프라이트·점수
│   ├── Fruit.cs            물리 오브젝트. 미리보기↔활성 상태 전환, 머지 트리거
│   ├── FruitSpawner.cs     마우스 추적 미리보기, 드롭, 다음 과일 준비
│   ├── GameManager.cs      싱글턴. fruitDatas 배열, 머지 처리, 게임오버
│   ├── UIManager.cs        싱글턴. 점수·다음과일·게임오버 패널
│   └── DangerZone.cs       위험선 Trigger. 2초 초과 시 게임오버
├── Data/                   FruitData ScriptableObject 11개 위치
├── Prefabs/                Fruit.prefab
└── Scenes/
    └── SampleScene.unity
Docs/
└── GDD.md                  기획서 (기준 문서)
```

## 코딩 규칙

- **포매터:** CSharpier (`dotnet csharpier`) — 저장 시 자동 실행 (훅 등록됨)
- **네임스페이스:** 없음 (소규모 프로젝트, 전역 클래스)
- **싱글턴 패턴:** `Awake`에서 `Instance != null → Destroy(gameObject)` 방식
- **물리:** `Rigidbody2D`, `CircleCollider2D`. 콜라이더 radius 고정 0.5, 크기는 `transform.scale`로 조절
- **UI:** uGUI + TextMeshPro. `점수: {score:N0}` 형식

## 핵심 데이터 흐름

```
GameManager.fruitDatas[0..10]
  → GetRandomSpawnData()  →  FruitSpawner (미리보기 표시)
  → SpawnFruit()          →  Fruit.Initialize() / SetAsPreview()
  → Drop()                →  Fruit.Activate()
  → OnCollisionEnter2D()  →  MergeFruits() → SpawnMerged() + AddScore()
  → DangerZone.Update()   →  TriggerGameOver()
```

## 구현 시 주의사항

- `FruitData.radius`는 **월드 단위 반지름**. `transform.scale = radius * 2`
- 머지 처리는 `instanceID`가 낮은 쪽에서만 진행 (이중 처리 방지)
- `SpawnMerged`는 `WaitForFixedUpdate` 후 실행 (물리 안정화)
- `maxRandomSpawnLevel = 4` → 드롭 과일은 레벨 0~4 (체리~감)까지만
- 게임오버 체크는 `isActive == true`인 과일만 대상

## 앞으로 할 일

`Docs/GDD.md` 섹션 8 참고.
