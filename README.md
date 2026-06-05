# 🎮 Uncovered : Remanent

> **캐주얼 & 저사양 유저를 위한 도심 배경 PVE 익스트렉션 FPS 슈터**

![Unity](https://img.shields.io/badge/Engine-Unity%206000.4.2f1-black?logo=unity)
![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp)
![Framework](https://img.shields.io/badge/Framework-NeoFPS-blueviolet)
![Period](https://img.shields.io/badge/Dev%20Period-2026.03%20~%202026.06-orange)
![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows)

---

## 📖 프로젝트 소개

*Escape From Tarkov* 흥행을 기점으로 익스트렉션 슈터 장르가 주목받기 시작했지만, 대부분의 타이틀은 하이엔드 PC를 기준으로 설계되어 진입 장벽이 높습니다.

**Uncovered : Remanent**는 이러한 문제를 정면으로 겨냥합니다. 익스트렉션 슈터 장르의 긴장감과 재미를 살리되, **캐주얼하고 저사양 친화적인 환경**에서도 충분히 즐길 수 있도록 설계된 싱글플레이어 전용 **PVE 익스트렉션 FPS** 입니다.

> 도심 한복판에 침투하고, 자원을 수집하고, 살아서 돌아오라.

---

## 🖼️ 스크린샷

| 메인 화면 | 레이드 (도시 맵) | 은신처 (Hideout) |
|:---------:|:----------------:|:----------------:|
| <img width="1918" height="1008" alt="Image" src="https://github.com/user-attachments/assets/2aa796a0-e69a-4e4a-80ca-3af83b1457e0" /> | <img width="1805" height="1016" alt="Image" src="https://github.com/user-attachments/assets/24e50cbf-4c21-4df2-b559-b5b22affa4d6" /> | <img width="1801" height="1007" alt="Image" src="https://github.com/user-attachments/assets/3fdc7bf3-aaf8-46b0-b168-c9f9ab8c7718" /> |

| 전투 장면 | 탈출 시퀀스 | 인벤토리 UI |
|:---------:|:-----------:|:-----------:|
| <img width="1813" height="1012" alt="Image" src="https://github.com/user-attachments/assets/1d830d68-f311-4b8a-bc2b-ebe2b1c200d8" /> | <img width="1805" height="1016" alt="Image" src="https://github.com/user-attachments/assets/23f74e4e-4a56-4e11-845e-c0805e2ae7b9" /> | <img width="1805" height="1012" alt="Image" src="https://github.com/user-attachments/assets/a4bf1ba7-cfae-4fed-ac91-6ffcd640b3b8" /> |


---

## 🎯 핵심 게임플레이

### 🏠 은신처 (Hideout)
플레이어의 안전한 거점이자 베이스 캠프입니다.

- 레이드에서 수집한 자원으로 **시설 업그레이드** 가능
- **무기 및 장비 성능 강화**
- 현재 구현된 시설: **방탄복 시설**, **스테이지 해금**, **총기 강화** 
- 레이드 후 이곳으로 복귀하여 아이템 정리 및 다음 출격 준비

### ⚔️ 레이드 (Raid)
자원 수집을 위해 적이 점령한 전투 지역에 침투합니다.

- 현재 구현 맵: **도시(City)**, **초반 지역**
- AI 적군이 유저를 추적하며 교전 시도
- **최소한의 피해**로 전투를 기피하는 전략적 플레이 권장
- 적의 위치를 파악하거나, 내가 수집해야 할 자원이 있는지 스스로 판단

### 🔑 탈출 시스템
살아서 돌아오는 것이 최우선입니다.

1. 해당 지역에서 확보한 아이템들을 가지고 탈출하려면 **열쇠 3개** 수집 필요
2. 근접 시 열쇠가 주변에 있다는 알림이 **UI에 표시**
3. 열쇠를 모두 수집하면 **탈출 지점 활성화**
4. 탈출 지점 도착 시 — 소지한 아이템 및 열쇠는 **자동 저장**, 수집 아이템은 **영구 보유**
5. 레이드 중 사망하면 수집한 아이템 일부 손실 위험

### 💊 아이템 활용
레이드 내에서 획득한 아이템으로:
- **체력 회복** (방탄복, 헬멧 등 장비 수리)
- 다음 레이드 준비를 위한 **내구 복구**

---

## 🛠️ 기술 스택

| 항목 | 내용 |
|------|------|
| **엔진** | Unity 6000.4.2f1 |
| **언어** | C# |
| **FPS 프레임워크** | NeoFPS — 플레이어 컨트롤 및 슈팅 기본 로직 |
| **데이터 관리** | 로컬 데이터 세이브 / 로드 시스템 |

---

## 👥 팀원 및 역할

| 이름 | 역할 | 담당 기능 |
|------|------|-----------|
| **송태훈** | 은신처 & 기본 로직 | 프로젝트 매니징/문서화, 은신처 기능 개발 및 UI 설계, 체력 및 방탄복 기능 구현 |
| **고범창** | AI & 시스템 | 적군 AI 개발, 인벤토리 시스템(기능 및 UI), 세이브&로드 로직, 은신처 기능 보완 |
| **고현아** | 레벨 디자인 & UI | 도심 맵 레벨 디자인, 탈출/열쇠 기믹 구현, 인벤토리 시스템 보조, 전체 UI 디자인 |

---

## 📁 프로젝트 구조 (주요 경로)

```
Assets/
├── Scenes/
│   ├── mainScreen.unity       # 메인 메뉴
│   ├── Hideout.unity          # 은신처
│   ├── CityMapScene.unity     # 도시 레이드 맵
│   ├── DemoCity.unity         # 데모 맵
│   └── TutorialScene.unity    # 튜토리얼
├── NeoFPS/                    # NeoFPS 프레임워크
└── ...
```

---

## 👀 LOGIC
<img width="3808" height="2177" alt="Image" src="https://github.com/user-attachments/assets/392938b2-4b16-46ab-9af9-1aac57d784f2" />

## 🚀 실행 방법

> 개발 환경 기준 실행 가이드입니다.

**요구 사항**
- Unity 6000.4.2f1
- Windows 10 / 11

**실행 순서**
```
1. 저장소 클론
   git clone https://github.com/{your-repo}/uncovered-remanent.git

2. Unity Hub에서 프로젝트 열기
   - Unity 버전: 6000.4.2f1

3. Build Settings에서 mainScreen 씬을 최상단으로 설정 후 실행
```

---

## 📅 개발 일정

| 기간 | 내용 |
|------|------|
| 2026.03 | 프로젝트 기획, 기술 스택 선정, NeoFPS 세팅 |
| 2026.04 | 핵심 시스템 개발 (은신처, AI, 인벤토리) |
| 2026.05 | 레이드 맵 레벨 디자인, 탈출 기믹, UI 구현 |
| 2026.06 | 통합 테스트, 버그 수정, 데모 빌드 완성 |


## 🔥 시연 영상
https://youtu.be/Bs93Zv1I3fI

---

<p align="center">
  <sub>© 2026 Uncovered : Remanent Team — 개발 기간 2026.03 ~ 2026.06</sub>
</p>

---
