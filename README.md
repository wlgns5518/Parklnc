## 💻시연 영상

https://youtu.be/H612a4RziP0

https://youtu.be/VSh1jeB8Dd8

### 프로젝트 개요 및 목표

- 현실적인 주차 시뮬레이션 요소(장애물 회피, 후진 등)를 통해 누구나 쉽게 즐기면서도 도전욕구를 자극하는 게임플레이 제공.
- 단순한 초급 주차 미션에서 시작하여 점점 복잡해지는 코스와 경로 생성으로 플레이어의 성장을 자연스럽게 유도.
- 모바일 환경에서 직관적으로 조작 가능한 UI 및 컨트롤 설계

## 🏆기술적인 도전 과제

- **타격 시스템: 각도 기반 애니메이션 + 타격 부위 표시**
    
    <aside>
    💡 타격 시스템
    
    ---
    
    ## ⁉ 타격 시스템이란!?
    
    **차량이나 오브젝트가 충돌 또는 피격되었을 때**, 충돌한 **각도에 따라 다른 애니메이션을 재생**하고, **타격 부위에 시각적인 효과(파티클)를 생성**하여 현실감을 높이는 시스템입니다.
    
    ## 🌟 **핵심 요소**
    
    - **타격 각도 계산**: 충돌 시점의 벡터 정보를 활용해 충돌 방향을 계산
    - **부위별 애니메이션 분기**: 방향에 따라 각기 다른 피격 반응 연출
    - **파티클 표시**: 충돌 지점에 정확히 효과 출력
    
    ---
    
    ## 🛠 **핵심 기술**
    
    ### 1. **충돌 각도 분석**
    
    ```csharp
    Vector3 contactNormal = collision.contacts[0].normal;
    Vector3 forward = transform.forward;
    contactNormal.y = 0;
    forward.y = 0;
    float angle = Vector3.SignedAngle(forward, contactNormal, Vector3.up);
    angle = (angle + 360f) % 360f;
    ```
    
    | 각도 범위 | 방향 | 적용 애니메이션 |
    | --- | --- | --- |
    | 0°~45°, 315°~360° | 정면 | `collidedId1` |
    | 45°~135° | 오른쪽 | `collidedId2` |
    | 135°~225° | 후면 | `collidedId3` |
    | 225°~315° | 왼쪽 | `collidedId4` |
    
    ---
    
    ### 2. **애니메이션 분기 재생**
    
    ```csharp
    
    carAnimator.SetTrigger(collidedId2); // 방향에 따라 Trigger 선택
    ```
    
    ---
    
    ### 3. **타격 지점 파티클 생성**
    
    ```csharp
    
    hit.transform.position = collision.contacts[0].point;
    hit.Play(); // 미리 설정된 ParticleSystem 재생
    ```
    
    - 충돌 방향(법선) 기준 회전 보정 가능
    - 성능 최적화를 위해 Play/Stop 호출만으로 재활용
    
    ---
    
    ## 💡 **활용 시나리오**
    
    - 차량이 장애물과 충돌했을 때 방향별 파손 애니메이션 제공
    - 격투 게임에서 타격 방향에 따라 적이 맞는 방향으로 반응
    - 총격 게임에서 총알이 박히는 방향에 따라 데칼 생성
    
    ---
    
    ## 📊 **기대 효과**
    
    | 항목 | 효과 |
    | --- | --- |
    | 리얼리즘 증가 | 실제 충돌처럼 보이는 시각적 반응 강화 |
    | 몰입감 향상 | 방향성 있는 피격 반응으로 몰입도 상승 |
    | 디버깅 편의 | 타격 위치 시각화로 테스트 효율 향상 |
    
    ---
    
    ## 🎯 **결론**
    
    **각도 기반 애니메이션 분기와 타격 부위 표시는**
    
    단순한 충돌 처리 이상의 **생동감 있는 피드백 시스템**으로,
    
    몰입형 주행 게임이나 액션 게임에서 **강력한 사용자 경험 향상 도구**로 작용합니다.
    
    </aside>
    
- A* 기반 경로 탐색 시스템
    
    <aside>
    💡 A*알고리즘
    
    ## ⁉ A*알고리즘이란?
    
    A* 알고리즘은 가장 효율적인 경로를 찾아주는 휴리스틱 기반의 탐색 알고리즘으로, 차량이 **장애물을 회피하며 목표 지점까지 이동할 수 있는 최적의 경로를 계산**하는 데 사용됩니다.
    
    ---
    
    ## 🌟 **핵심 요소**
    
    - **A* 알고리즘 기반 경로 탐색**
    - **경로 회전 수에 따라 난이도 결정**
    - **경로 주변만 선별적으로 장애물 배치**
    - **오브젝트 풀링 기반 장애물 재사용**
    
    ---
    
    ## 🛠 **핵심 기술**
    
    ### 1. **레벨에 맞는 경로 생성 (턴 수 기반)**
    
    ```csharp
    
    List<Vector2Int> path = FindPathWithExactTurns(level);
    ```
    
    - Level 1: `L자형 고정 경로`
    - Level ≥ 2: A* 알고리즘 기반 `회전 수를 조절한 경로 생성`
    
    ### 📌 회전 수 계산 방식
    
    ```csharp
    
    int CountTurns(List<Vector2Int> path)
    {
        int turns = 0;
        Vector2Int prevDir = path[1] - path[0];
        for (int i = 2; i < path.Count; i++)
        {
            Vector2Int dir = path[i] - path[i - 1];
            if (dir != prevDir)
            {
                turns++;
                prevDir = dir;
            }
        }
        return turns;
    }
    ```
    
    ---
    
    ### 2. **A* 알고리즘 + 랜덤 가중치**
    
    ```csharp
    
    float tentativeG = gScore[current] + 1 + randomWeight;
    ```
    
    - **랜덤 가중치**를 추가해 다양한 경로 생성
    - `mustInclude` 지점 (예: (1,0)) 포함하여 초반 경로 통제
    
    ---
    
    ### 3. **격자 기반 장애물 배치**
    
    ```csharp
    
    HashSet<Vector2Int> obstacleCandidates;
    foreach (var cell in path)
    {
        foreach (var neighbor in GetNeighbors(cell))
            if (!pathSet.Contains(neighbor))
                obstacleCandidates.Add(neighbor);
    }
    ```
    
    - 경로 주변만 후보로 삼아 장애물 배치
    - 맵 전체에 무작위로 배치하는 방식보다 **난이도 조절 및 퍼포먼스 우수**
    
    ---
    
    ### 4. **Object Pooling 기반 장애물 배치**
    
    ```csharp
    
    GameObject obj = objectPool.Get(obstacle.prefab, transform);
    obj.transform.position = new Vector3(worldPos.x, 0f, worldPos.y);
    ```
    
    - Instantiate 없이 미리 생성된 오브젝트 재사용
    - `objectPool.Preload()`로 사전 준비
    - 충돌 없이 부드러운 장애물 배치 가능
    
    ---
    
    ## 💡 **활용 시나리오**
    
    - 경로 주행 퍼즐 게임에서 난이도별 주행 루트 제공
    - 코너가 많을수록 난이도가 높아지는 설계 적용
    - 맵을 매번 새롭게 생성하는 **재플레이성 높은 콘텐츠 구성**
    
    ---
    
    ## 📊 **기대 효과**
    
    | 항목 | 효과 |
    | --- | --- |
    | 경로 중심 장애물 배치 | 플레이에 영향 주는 배치만 하여 설계 난이도 향상 |
    | 난이도 유동성 확보 | 회전 수 기반으로 자연스럽게 난이도 조절 가능 |
    | 성능 최적화 | Object Pool 사용으로 Instantiate 성능 부담 제거 |
    
    ---
    
    ## 🎯 **결론**
    
    **회전 수 기반 A* 경로 생성과 오브젝트 풀링 장애물 배치 시스템은**
    
    퍼포먼스를 확보하면서도 레벨 디자인 유연성을 극대화합니다.
    
    자동 생성된 경로에 자연스럽게 도전 요소를 배치하여,
    
    **주행 퍼즐 게임의 몰입감과 리플레이 가치를 높이는 핵심 기술**입니다.
    
    </aside>
