using UnityEngine;
using System;
using System.Collections.Generic;

namespace Mkey
{
    public class SlotGroupBehavior : MonoBehaviour
    {
        public List<int> symbOrder; // 릴에 표시될 심볼의 순서를 정의하는 리스트
        public List<Triple> triples; // 세 개의 연속된 심볼 조합을 저장하는 리스트
        [SerializeField]
        [Tooltip("Symbol windows, from top to bottom")]
        private RayCaster[] rayCasters; // 심볼을 감지하는 레이캐스터 배열(위에서 아래로 정렬됨)


        [Space(16, order = 0)]
        [SerializeField]
        [Tooltip("sec, additional rotation time")]
        private float addRotateTime = 0f;
        [SerializeField]
        [Tooltip("sec, delay time for spin")]
        private float spinStartDelay = 0f;
        [Tooltip("min 0% - max 20%, change spinStartDelay")]
        [SerializeField]
        private int spinStartRandomize = 0;
        [SerializeField]
        private int spinSpeedMultiplier = 3;

        [Space(16, order = 0)]
        [SerializeField]
        [Tooltip("If true - reel set to random position at start")]
        private bool randomStartPosition = false; // 시작 시 릴을 랜덤 위치에 배치할지 여부
        [Space(16, order = 0)]
        [SerializeField]
        [Tooltip("Tile size by Y")]
        private float tileSizeY = 3.13f; // 타일(심볼)의 Y축 크기
        [SerializeField]
        [Tooltip("Additional space between tiles")]
        private float gapY = 0.35f; // 타일 사이의 추가 간격
        [SerializeField]
        [Tooltip("Link to base (bottom raycaster)")]
        private bool baseLink = false; // 하단 레이캐스터에 연결할지 여부

        #region simulate
        [SerializeField]
        private bool simulate = false; // 시뮬레이션 모드 활성화 여부
        [SerializeField]
        public int simPos = 0; // 시뮬레이션 위치
        #endregion simulate

        [Tooltip("ReelSymbols source")]
        public SlotGroupBehavior CopyFrom;

        #region temp vars
        private float anglePerTileRad = 0; // 타일당 각도(라디안)
        private float anglePerTileDeg = 0; // 타일당 각도(도)
        private TweenSeq tS; // 트윈 시퀀스
        private Transform TilesGroup; // 타일 그룹 트랜스폼
        private SlotSymbol[] slotSymbols; // 슬롯 심볼 배열
        private SlotIcon[] sprites; // 심볼 아이콘 배열

        private int lastChanged = -1; // 마지막으로 변경된 심볼 인덱스
        [SerializeField]
        private bool debugreel=false; // 디버그 모드 활성화 여부
        private int tileCount; // 타일 개수
        private int windowSize; // 창 크기
        private int maxWindowSize=5; // 최대 창 크기
        [SerializeField]
        private int topSector = 0; // 최상단 섹터
        private int tempSectors = 0; // 임시 섹터 카운터
        #endregion temp vars

        #region properties 
        public int NextOrderPosition { get; private set; }
        public int CurrOrderPosition { get; private set; }
        public RayCaster[] RayCasters { get { return rayCasters; } }
        #endregion properties 

        #region dev
        public string orderJsonString;
        #endregion dev

        #region regular
        // 인스펙터에서 값 변경 시 유효성 검사
        private void OnValidate()
        {
            spinStartRandomize = (int)Mathf.Clamp(spinStartRandomize, 0, 20);
            spinStartDelay = Mathf.Max(0, spinStartDelay);
            spinSpeedMultiplier = Mathf.Max(0, spinSpeedMultiplier);
            addRotateTime = Mathf.Max(0, addRotateTime);
        }

        // 객체 파괴 시 회전 취소
        private void OnDestroy()
        {
            CancelRotation();
        }

        // 객체 비활성화 시 회전 취소
        private void OnDisable()
        {
            CancelRotation();
        }
        #endregion regular

        // 각 심볼의 출현 확률
        public float[] SymbProbabilities
        {
            get; private set;
        }

        /// <summary>
        /// 슬롯 실린더(릴) 생성 - 심볼들을 원통형으로 배치
        /// </summary>
        /// <param name="sprites">심볼 아이콘 배열</param>
        /// <param name="tileCount">타일 총 개수</param>
        /// <param name="tilePrefab">타일 프리팹</param>
        internal void CreateSlotCylinder(SlotIcon[] sprites, int tileCount, GameObject tilePrefab)
        {
            CurrOrderPosition = 0; // 현재 위치 초기화
            this.sprites = sprites; // 심볼 아이콘 배열 저장
            this.tileCount = tileCount; // 타일 개수 저장
            slotSymbols = new SlotSymbol[tileCount]; // 심볼 배열 초기화

            // 릴 트랜스폼 생성
            TilesGroup = (new GameObject()).transform; // 새로운 GameObject 생성 및 Transform 가져오기
            TilesGroup.localScale = transform.lossyScale; // 생성한 오브젝트의 스케일을 현재 객체의 lossless(월드 스케일) 스케일로 설정
            TilesGroup.parent = transform; // 현재 오브젝트의 자식으로 설정
            TilesGroup.localPosition = Vector3.zero; // 자식 오브젝트의 로컬 위치를 원점으로 설정 -> 부모(현재 오브젝트)와 동일한 위치에 배치
            TilesGroup.name = "Reel(" + name + ")";

            // 릴 기하학 계산
            float distTileY = tileSizeY + gapY; // 심볼 간 거리 = 타일 크기 + 간격

            // 각도 계산 (원 둘레에 타일들을 균등하게 배치)
            anglePerTileDeg = 360.0f / (float)tileCount; // 타일당 각도(도)
            anglePerTileRad = anglePerTileDeg * Mathf.Deg2Rad; // 타일당 각도(라디안)

            // 반지름 계산 - 타일 간 거리를 유지하면서 원형 배치를 위한 반지름
            float radius = (distTileY / 2f) / Mathf.Tan(anglePerTileRad / 2.0f); //old float radius = ((tileCount + 1) * distTileY) / (2.0f * Mathf.PI);

            windowSize = rayCasters.Length; // 창 크기 = 레이캐스터 개수

            // 시작 각도 계산 (레이캐스터 위치에 맞게 조정)
            bool isEvenRayCastersCount = (windowSize % 2 == 0); // 레이캐스터 개수가 짝수인지
            int dCount = (isEvenRayCastersCount) ? windowSize / 2 - 1 : windowSize / 2;
            float addAnglePerTileDeg = (isEvenRayCastersCount) ? -anglePerTileDeg * dCount - anglePerTileDeg / 2f : -anglePerTileDeg;
            float addAnglePerTileRad = (isEvenRayCastersCount) ? -anglePerTileRad * dCount - anglePerTileRad / 2f : -anglePerTileRad;
            topSector = windowSize - 1; // 최상단 섹터 설정

            // 릴 위치 Z축 조정 (반지름만큼 이동)
            TilesGroup.localPosition = new Vector3(TilesGroup.localPosition.x, TilesGroup.localPosition.y, radius); // offset reel position by z-coordinat

            // 하단 레이캐스터 기준 조정 (baseLink가 true일 경우)
            RayCaster baseRC = rayCasters[rayCasters.Length - 1]; // 하단 레이캐스터
            float brcY = baseRC.transform.localPosition.y;
            float dArad = 0f;

            if (brcY > -radius && brcY < radius && baseLink)
            {
                // 하단 레이캐스터 위치에 맞게 회전 각도 조정
                float dY = brcY - TilesGroup.localPosition.y;
                dArad = Mathf.Asin(dY/radius);
            //    Debug.Log("dY: "+ dY + " ;dArad: " + dArad  + " ;deg: " + dArad* Mathf.Rad2Deg);
                addAnglePerTileRad = dArad;
                addAnglePerTileDeg = dArad * Mathf.Rad2Deg;
            }
            else if(baseLink)
            {
                Debug.Log("Base Rc position out of reel radius" ); // 에러: 레이캐스터가 반지름 범위 밖에 있음
            }

            // 릴 타일 생성
            for (int i = 0; i < tileCount; i++)
            {
                float n = (float)i;

                // 각 타일의 회전 각도 계산
                float tileAngleRad = n * anglePerTileRad + addAnglePerTileRad; // '- anglePerTileRad' -  symborder corresponds to visible symbols on reel before first spin 
                float tileAngleDeg = n * anglePerTileDeg + addAnglePerTileDeg;

                // 타일 생성 및 위치 설정
                slotSymbols[i] = Instantiate(tilePrefab, TilesGroup).GetComponent<SlotSymbol>();
                
                // 원통 표면에 타일 배치 (삼각함수 이용)
                slotSymbols[i].transform.localPosition = new Vector3(0, radius * Mathf.Sin(tileAngleRad), -radius * Mathf.Cos(tileAngleRad));
                slotSymbols[i].transform.localEulerAngles = new Vector3(tileAngleDeg, 0, 0);
               
                // 타일 회전 (표면에 수직하도록)
                slotSymbols[i].name = "SlotSymbol: " + String.Format("{0:00}", i);
            }

            // 심볼 설정
            for (int i = 0; i < tileCount; i++)
            {
                int symNumber = symbOrder[GetNextSymb()]; // 다음 심볼 ID 가져오기
                slotSymbols[i].SetIcon(sprites[symNumber], symNumber); // 심볼 설정
            }
            lastChanged = tileCount - 1; // 마지막으로 변경된 심볼 인덱스 설정

            // 심볼 확률 계산
            SymbProbabilities = GetReelSymbHitPropabilities(sprites);
            CurrOrderPosition = 0; // 현재 위치 재설정  '- anglePerTileRad' - 

            // 랜덤 시작 위치 설정 (옵션)
            if (randomStartPosition)
            {
                NextOrderPosition = UnityEngine.Random.Range(0, symbOrder.Count); // 랜덤 위치 선택
                float angleX = GetAngleToNextSymb(NextOrderPosition); // 해당 위치까지의 각도 계산
                topSector += Mathf.Abs(Mathf.RoundToInt(angleX / anglePerTileDeg)); // 범위 내로 조정
                topSector = (int)Mathf.Repeat(topSector, tileCount);
                TilesGroup.Rotate(-angleX, 0, 0); // 해당 위치로 회전
                CurrOrderPosition = NextOrderPosition; // 현재 위치 업데이트
                WrapSymbolTape((-angleX)); // 심볼 테이프 래핑
            }
            if (debugreel) SignTopSymbol(topSector); // 디버그: 최상단 심볼 표시
        }

        /// <summary>
        /// 비동기 실린더(릴) 회전 애니메이션 실행 - 4단계 애니메이션 (시작, 연속, 주요, 종료)
        /// </summary>
        /// <param name="mainRotType">주요 회전 이징 타입</param>
        /// <param name="inRotType">시작 회전 이징 타입</param>
        /// <param name="outRotType">종료 회전 이징 타입</param>
        /// <param name="mainRotTime">주요 회전 시간</param>
        /// <param name="mainRotateTimeRandomize">주요 회전 시간 랜덤화 비율</param>
        /// <param name="inRotTime">시작 회전 시간</param>
        /// <param name="outRotTime">종료 회전 시간</param>
        /// <param name="inRotAngle">시작 회전 각도</param>
        /// <param name="outRotAngle">종료 회전 각도</param>
        /// <param name="nextOrderPosition">다음 정지 위치</param>
        /// <param name="rotCallBack">회전 완료 콜백</param>
        internal void NextRotateCylinderEase(EaseAnim mainRotType, EaseAnim inRotType, EaseAnim outRotType,
                                        float mainRotTime, float mainRotateTimeRandomize,
                                        float inRotTime, float outRotTime,
                                        float inRotAngle, float outRotAngle,
                                        int nextOrderPosition,  Action rotCallBack)

        {
#if UNITY_ANDROID || UNITY_IOS
            mainRotTime *= 0.8f;  // 모바일에서는 애니메이션 속도를 20% 빠르게
#endif

            // 시뮬레이션 모드면 지정된 위치 사용, 아니면 전달된 위치 사용
            NextOrderPosition = (!simulate) ? nextOrderPosition : simPos;

            // 회전 시작 지연 시간 계산
            spinStartDelay = Mathf.Max(0, spinStartDelay); // 음수 방지
            float spinStartRandomizeF = Mathf.Clamp(spinStartRandomize / 100f, 0f, 0.2f); // 0-0.2 범위로 변환
            float startDelay = UnityEngine.Random.Range(spinStartDelay * (1.0f - spinStartRandomizeF), spinStartDelay * (1.0f + spinStartRandomizeF)); // 랜덤 지연 계산

            // check range before start: 안전한 범위로 값 제한
            inRotTime = Mathf.Clamp(inRotTime, 0, 1f); // 시작 회전 시간 제한
            inRotAngle = Mathf.Clamp(inRotAngle, 0, 10); // 시작 회전 각도 제한

            outRotTime = Mathf.Clamp(outRotTime, 0, 1f); // 종료 회전 시간 제한
            outRotAngle = Mathf.Clamp(outRotAngle, 0, 10); // 종료 회전 각도 제한

            // 회전 시퀀스 생성 (4단계: 시작-연속-주요-종료)
            float oldVal = 0f; // 이전 값 (애니메이션 델타 계산용)
            tS = new TweenSeq(); // 새 트윈 시퀀스 생성
            float angleX = 0; // 총 회전 각도
            tempSectors = 0; // 임시 섹터 카운터 초기화

            // 1. 시작 회전 부분 (가속)
            tS.Add((callBack) => // in rotation part
            {
                SimpleTween.Value(gameObject, 0f, inRotAngle, inRotTime)
                                  .SetOnUpdate((float val) =>
                                  {
                                      TilesGroup.Rotate(val - oldVal, 0, 0); // 증분만큼 회전
                                      oldVal = val; // 이전 값 업데이트
                                  })
                                  .AddCompleteCallBack(() =>
                                  {
                                     callBack(); // 완료 콜백
                                  }).SetEase(inRotType).SetDelay(startDelay); // 이징 및 지연 설정
            });

            // 2. 연속 회전 부분 (옵션)
            if (NextOrderPosition == -1) // 특수 값 -1은 연속 회전 의미
                tS.Add((callBack) => // 연속 회전 추가
                {
                    RecurRotation(mainRotTime / 1.0f, callBack); // 재귀적 회전 시작
                });

            // 3. 주요 회전 부분
            tS.Add((callBack) =>  // main rotation part
            {
                oldVal = 0f; // 이전 값 초기화
                addRotateTime = Mathf.Max(0, addRotateTime);  // 음수 방지
                mainRotateTimeRandomize = Mathf.Clamp(mainRotateTimeRandomize, 0f, 0.2f); // 범위 제한

                // 회전 시간 랜덤화
                mainRotTime = addRotateTime + UnityEngine.Random.Range(mainRotTime * (1.0f - mainRotateTimeRandomize), mainRotTime * (1.0f + mainRotateTimeRandomize));

                spinSpeedMultiplier = Mathf.Max(0, spinSpeedMultiplier); // 음수 방지

                // 목표 위치까지의 각도 + 추가 회전 (배수만큼 더 회전)
                angleX = GetAngleToNextSymb(NextOrderPosition) + anglePerTileDeg * symbOrder.Count * spinSpeedMultiplier;
                if(debugreel) Debug.Log(name + ", angleX : " + angleX); // 디버그 로그

                // 주요 회전 애니메이션 실행
                SimpleTween.Value(gameObject, 0, -(angleX + outRotAngle + inRotAngle), mainRotTime)
                                  .SetOnUpdate((float val) =>
                                  {
                                      // 회전 각도만큼 증분 회전
                                      // check rotation angle 
                                      TilesGroup.Rotate(val - oldVal, 0, 0);
                                      oldVal = val;

                                      // 특정 범위 내에서 심볼 테이프 래핑 실행
                                      if (val < -inRotAngle && val >= -(angleX + inRotAngle))
                                          WrapSymbolTape(val + inRotAngle);
                                  })
                                  .AddCompleteCallBack(() =>
                                  {
                                      // 최종 심볼 테이프 래핑
                                      WrapSymbolTape(angleX);

                                      // 최상단 섹터 업데이트
                                      topSector += Mathf.Abs(Mathf.RoundToInt(angleX / anglePerTileDeg));
                                      topSector = (int)Mathf.Repeat(topSector, tileCount); // 범위 내로 조정
                                      if (debugreel) SignTopSymbol(topSector); // 디버그: 최상단 심볼 표시

                                      callBack(); // 완료 콜백
                                  }).SetEase(mainRotType); // 이징 설정
            });

            // 4. 종료 회전 부분 (감속)
            tS.Add((callBack) =>  // out rotation part
            {
                oldVal = 0f; // 이전 값 초기화
                SimpleTween.Value(gameObject, 0, outRotAngle, outRotTime)
                                  .SetOnUpdate((float val) =>
                                  {
                                      TilesGroup.Rotate(val - oldVal, 0, 0); // 증분만큼 회전
                                      oldVal = val; // 이전 값 업데이트
                                  })
                                  .AddCompleteCallBack(() =>
                                  {
                                      CurrOrderPosition = NextOrderPosition; // 현재 위치 업데이트
                                      rotCallBack?.Invoke(); // 전체 회전 완료 콜백
                                      callBack(); // 단계 완료 콜백
                                  }).SetEase(outRotType); // 이징 설정
            });

            // 시퀀스 시작
            tS.Start();
        }

        /// <summary>
        /// 재귀적 연속 회전 - 정지 명령이 있을 때까지 계속 회전
        /// </summary>
        /// <param name="rotTime">회전 시간</param>
        /// <param name="completeCallBack">완료 콜백</param>
        private void RecurRotation(float rotTime, Action completeCallBack)
        {
            float newAngle = -anglePerTileDeg * symbOrder.Count; // 한 바퀴 회전 각도
            tempSectors = 0; // 임시 섹터 카운터 초기화
            float oldVal = 0; // 이전 값 (애니메이션 델타 계산용)

            // 선형 회전 애니메이션 실행
            SimpleTween.Value(gameObject, 0, newAngle, rotTime)
                                .SetOnUpdate((float val) =>
                                {
                                    if (this) // 객체 존재 확인
                                    {
                                        TilesGroup.Rotate(val - oldVal, 0, 0); // 증분만큼 회전
                                        oldVal = val; // 이전 값 업데이트
                                        WrapSymbolTape(val); // 심볼 테이프 래핑
                                    }
                                })
                                .AddCompleteCallBack(() =>
                                {
                                    // 최종 심볼 테이프 래핑
                                    WrapSymbolTape(newAngle);

                                    tempSectors = 0; // 임시 섹터 카운터 초기화
                                    topSector += symbOrder.Count; // 최상단 섹터 업데이트
                                    topSector = (int)Mathf.Repeat(topSector, tileCount); // 범위 내로 조정

                                    // 계속 회전(-1) 상태면 재귀 호출, 아니면 완료
                                    if (NextOrderPosition == -1) RecurRotation(rotTime, completeCallBack);
                                    else {completeCallBack?.Invoke(); }
                                }).SetEase(EaseAnim.EaseLinear); // 선형 이징 (일정 속도)
        }

        /// <summary>
        /// 심볼 테이프 래핑 - 회전 중 보이지 않는 영역의 심볼을 변경
        /// </summary>
        /// <param name="dA">회전 각도</param>
        private void WrapSymbolTape(float dA)
        {
            // 회전한 섹터 수 계산
            int sectors = Mathf.Abs(Mathf.RoundToInt(dA / anglePerTileDeg));
          //  if (sectors < tileCount-windowSize-2) return;

            bool found = false; // 마지막 변경 심볼 찾음 여부

            for (int i = topSector + tempSectors; i < topSector + sectors + 3; i++)
            {
                int ip = (int)Mathf.Repeat(i, tileCount);  // 인덱스 범위 내로 조정
                tempSectors = i - topSector; // 임시 섹터 카운터 업데이트

                if (!found)
                {
                    found = (ip == lastChanged); // 마지막으로 변경된 심볼 찾기
                }
                else // 마지막 변경 심볼 이후 새 심볼 설정
                {
                    if (debugreel) Debug.Log("found: " + found);
                    int symNumber = symbOrder[GetNextSymb()]; // 다음 심볼 ID 가져오기
                    slotSymbols[ip].SetIcon(sprites[symNumber], symNumber); // 심볼 아이콘 설정
                    lastChanged = ip; // 마지막 변경 심볼 인덱스 업데이트

                    // 디버그 로그
                    if (debugreel) Debug.Log("set symbol in: " + ip + "; tempsectors: " + tempSectors);
                }
            }
        }

        int next = 0; // 다음 심볼 순서 인덱스
        /// <summary>
        /// symbOrder 배열에서 다음 심볼 위치 반환
        /// </summary>
        /// <returns>다음 심볼 순서 인덱스</returns>
        private int GetNextSymb()
        {
            return (int)Mathf.Repeat(next++, symbOrder.Count); // 순환적으로 다음 인덱스 반환
        }

        /// <summary>
        /// symbOrder 배열에서 다음 심볼 위치까지의 각도(도) 반환
        /// </summary>
        /// <param name="nextOrderPosition">다음 목표 위치</param>
        /// <returns>회전해야 할 각도</returns>
        private float GetAngleToNextSymb(int nextOrderPosition)
        {
            if (CurrOrderPosition < nextOrderPosition)
            {
                // 현재 위치보다 앞에 있는 경우 (정방향 회전)
                return (nextOrderPosition - CurrOrderPosition) * anglePerTileDeg;
            }

            // 현재 위치보다 뒤에 있는 경우 (한 바퀴 돌아서 도달)
            return (symbOrder.Count - CurrOrderPosition + nextOrderPosition) * anglePerTileDeg;
        }

        /// <summary>
        /// symbOrder 배열에 따른 각 심볼의 출현 확률 반환
        /// </summary>
        /// <returns>각 심볼의 출현 확률 배열</returns>
        internal float[] GetReelSymbHitPropabilities(SlotIcon[] symSprites)
        {
            if (symSprites == null || symSprites.Length == 0)
                return null;

            float[] probs = new float[symSprites.Length]; // 확률 배열 초기화
            int length = symbOrder.Count;
            for (int i = 0; i < length; i++)
            {
                int n = symbOrder[i];
                probs[n]++;
            }
            for (int i = 0; i < probs.Length; i++)
            {
                probs[i] = probs[i] / (float)length;
            }
            return probs;
        }

        /// <summary>
        /// Return true if top, middle or bottom raycaster has symbol with ID == symbID
        /// </summary>
        /// <param name="symbID"></param>
        /// <returns></returns>
        public bool HasSymbolInAnyRayCaster(int symbID, ref List<SlotSymbol> slotSymbols)
        {
            slotSymbols = new List<SlotSymbol>();
            bool res = false;
            SlotSymbol sS;

            for (int i = 0; i < rayCasters.Length; i++)
            {
                sS = rayCasters[i].GetSymbol();
                if (sS.IconID == symbID)
                {
                    res = true;
                    slotSymbols.Add(sS);
                }
            }

            return res;
        }

        /// <summary>
        /// Set next reel order while continuous rotation
        /// </summary>
        /// <param name="r"></param>
        internal void SetNextOrder(int r)
        {
            if (NextOrderPosition == -1)
                NextOrderPosition = r;
        }

        internal void CancelRotation()
        {
            SimpleTween.Cancel(gameObject, false);
            if (tS != null) tS.Break();
        }

        #region calculate
        public void CreateTriples()
        {
            triples = new List<Triple>();
            for (int i = 0; i < symbOrder.Count; i++)
            {
                int f = symbOrder[i];
                int s = symbOrder[(int)Mathf.Repeat(i + 1, symbOrder.Count)];
                int t = symbOrder[(int)Mathf.Repeat(i + 2, symbOrder.Count)];
                Triple triple = new Triple(new List<int> { f, s, t }, symbOrder.Count - 1);
                triples.Add(triple);
            }
        }

        public Triple GetRandomTriple()
        {
            int i = UnityEngine.Random.Range(0, symbOrder.Count);

            int f = symbOrder[i];
            int s = symbOrder[(int)Mathf.Repeat(i + 1, symbOrder.Count)];
            int t = symbOrder[(int)Mathf.Repeat(i + 2, symbOrder.Count)];

            return new Triple(new List<int> { f, s, t }, symbOrder.Count - 1);
        }
        #endregion calculate

        #region dev
        public void OrderTostring()
        {
            string res = "";
            for (int i = 0; i < symbOrder.Count; i++)
            {
                res += (i + ") ");
                res += symbOrder[i];
                if (i < symbOrder.Count - 1) res += "; ";
            }

            Debug.Log(res);
        }

        private void SignTopSymbol(int top)
        {
            for (int i = 0; i < slotSymbols.Length; i++)
            {
                if (slotSymbols[i].name.IndexOf("Top")!=-1) slotSymbols[i].name = "SlotSymbol: " + String.Format("{0:00}", i);
            }

            slotSymbols[top].name = "Top - " + slotSymbols[top].name;
        }

        public int GetRaycasterIndex(RayCaster rC)
        {
            int res = -1;
            if (!rC) return res;
            for (int i = 0; i < RayCasters.Length; i++)
            {
                if (RayCasters[i] == rC) return i;
            }
            return res;
        }

        public string CheckRaycasters()
        {
            string res = "";
            if (RayCasters == null || RayCasters.Length == 0) return "need to setup raycasters";

            for (int i = 0; i < RayCasters.Length; i++)
            {
                if (!RayCasters[i]) res += (i + ")raycaster - null; ");
                else { res += (i+ ")"+ RayCasters[i].name + "; " ); }
            }
            return res;
        }

        public void SetDefaultChildRaycasters()
        {
            RayCaster[] rcs = GetComponentsInChildren<RayCaster>(true);
            rayCasters = rcs;

        }

        public string OrderToJsonString()
        {
            string res = "";
            ListWrapperStruct<int> lW = new ListWrapperStruct<int>(symbOrder);
            res = JsonUtility.ToJson(lW);
            return res;
        }

        public void SetOrderFromJson()
        {
            Debug.Log("Json viewer - " + "http://jsonviewer.stack.hu/");
            Debug.Log("old reel symborder json: " + OrderToJsonString());

            if (string.IsNullOrEmpty(orderJsonString))
            {
                Debug.Log("orderJsonString : empty");
                return;
            }

            ListWrapperStruct<int> lWPB = JsonUtility.FromJson<ListWrapperStruct<int>>(orderJsonString);
            if (lWPB != null && lWPB.list != null && lWPB.list.Count > 0)
            {
                symbOrder = lWPB.list;
            }
        }
        #endregion dev
    }

    [Serializable]
    public class Triple
    {
        public List<int> ordering;
        public int number;

        public Triple(List<int> ordering, int number)
        {
            this.ordering = new List<int>(ordering);
            this.number = number;
        }

        public override string ToString()
        {
            string res = "";
            for (int i = 0; i < ordering.Count; i++)
            {
                res += ordering[i];
                if (i < ordering.Count - 1) res += ", ";
            }
            return res;
        }
    }
}
