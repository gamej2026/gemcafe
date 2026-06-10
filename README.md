# SelfHostTest
 빌드 테스트
Unity WebGL 프로젝트를 GitHub Pages로 배포하는 테스트 프로젝트입니다.
 빌드 
## 🌐 GitHub Pages 배포

이 프로젝트는 GitHub Actions를 사용하여 자동으로 GitHub Pages에 배포됩니다.

### 배포된 사이트 확인

배포된 Unity WebGL 게임은 다음 URL에서 확인할 수 있습니다:
- **URL**: https://gamej2026.github.io/[저장소이름]/

### GitHub Pages 활성화 방법

처음 배포하는 경우, 다음 단계를 따라 GitHub Pages를 활성화해야 합니다:

1. GitHub 저장소로 이동: https://github.com/gamej2026/[저장소이름]
2. **Settings** (설정) 탭 클릭
3. 왼쪽 메뉴에서 **Pages** 클릭
4. **Source** 섹션에서 **GitHub Actions** 선택
5. 저장

### 자동 빌드 및 배포

- `main` 브랜치에 push하면 자동으로 빌드 및 배포됩니다
- **빌드 과정**:
  1. Self-hosted runner에서 로컬 Unity를 사용하여 WebGL 빌드 실행
  2. 빌드된 파일의 Gzip 압축 해제 (GitHub Pages 호환성)
  3. 빌드 결과물을 아티팩트로 저장
- **배포 과정**:
  1. GitHub-hosted runner (ubuntu-latest)에서 빌드 아티팩트 다운로드
  2. GitHub Pages에 배포
- Actions 탭에서 빌드 및 배포 상태를 확인할 수 있습니다

### Self-Hosted Runner 설정

이 프로젝트는 self-hosted runner를 사용하여 빌드를 수행합니다.

**요구 사항:**
1. Windows 또는 macOS 기반 self-hosted runner가 GitHub 저장소에 등록되어 있어야 합니다
2. Unity 라이선스가 활성화되어 있어야 합니다 (아래 참조)

**Self-Hosted Runner 등록 방법:**
1. GitHub 저장소로 이동: https://github.com/gamej2026/[저장소이름]
2. **Settings** (설정) 탭 클릭
3. 왼쪽 메뉴에서 **Actions** → **Runners** 클릭
4. **New self-hosted runner** 버튼 클릭
5. 운영체제(Windows 또는 macOS)를 선택하고 제공되는 명령어를 실행하여 runner 설치 및 등록

**Unity 라이선스 설정:**

Self-hosted runner에서 Unity를 배치 모드로 실행하려면 라이선스가 필요합니다. 다음 두 가지 방법 중 하나를 선택하세요:

**옵션 1: EMAIL과 PASSWORD를 사용한 자동 활성화 (권장)**

Unity Personal 라이선스는 EMAIL과 PASSWORD를 사용하여 자동으로 라이선스를 활성화할 수 있습니다:

1. GitHub 저장소의 Settings → Secrets and variables → Actions로 이동
2. 다음 시크릿을 추가합니다:
   - **UNITY_EMAIL**: Unity 계정 이메일 주소
   - **UNITY_PASSWORD**: Unity 계정 비밀번호
3. 워크플로우가 자동으로 빌드 전에 라이선스를 활성화합니다:
   - **Windows**: `unity-activate` 도구를 사용하여 수동 활성화 파일(.ulf)을 생성하고 활성화
   - **macOS**: Unity의 `-username`/`-password` 옵션을 사용하여 직접 활성화 (Personal 라이선스 지원)
4. 참고: Google 계정으로 로그인하는 경우, Unity 계정 설정에서 비밀번호를 직접 설정해야 합니다
5. 참고: 2FA가 활성화된 경우, 추가 설정이 필요할 수 있습니다

**옵션 2: Self-hosted runner 머신에서 Unity 수동 활성화**

1. Self-hosted runner 머신에서 Unity Editor를 실행
2. Unity 계정으로 로그인하여 라이선스 활성화
3. 이후 GitHub Actions 워크플로우가 자동으로 해당 라이선스를 사용

**중요**: Unity Personal 라이선스는 무료이지만 수익 기준이 있습니다. Unity Plus/Pro 라이선스를 사용하는 경우에도 EMAIL/PASSWORD 방식이나 수동 활성화 방식을 사용할 수 있습니다.

## 🎮 프로젝트 정보

- **Build Target**: WebGL
- **Description**: 셀프 호스팅으로 Action 테스트 되는지 확인

## 📁 프로젝트 구조

```
.
├── Assets/              # Unity 에셋 파일
│   ├── Editor/         # 에디터 스크립트
│   │   └── BuildScript.cs  # WebGL 빌드 스크립트
│   ├── Scripts/        # 게임 로직 스크립트
│   └── Scenes/         # Unity 씬
└── .github/
    └── workflows/
        └── build-and-deploy.yml  # GitHub Actions 워크플로우 (빌드 + 배포)
```

## 🔧 로컬 빌드

Unity Editor에서 빌드하려면:

1. Unity 설치
2. 프로젝트 열기
3. File → Build Settings → WebGL 선택
4. Build 클릭 또는 BuildScript를 사용하여 빌드

## ⚠️ GitHub Pages 배포 시 주의사항

### Gzip 압축 문제 해결

Unity WebGL 빌드가 Gzip 압축을 사용하는 경우, GitHub Pages에서 다음과 같은 에러가 발생할 수 있습니다:

```
Unable to parse Build/build.framework.js.gz! This can happen if build compression was enabled but web server hosting the content was misconfigured to not serve the file with HTTP Response Header "Content-Encoding: gzip" present.
```

**원인**: GitHub Pages는 `.gz` 파일을 제공할 때 `Content-Encoding: gzip` 헤더를 자동으로 설정하지 않아, Unity 로더가 압축된 파일을 올바르게 처리하지 못합니다.

**해결 방법**: 이 저장소의 GitHub Actions 워크플로우는 빌드 후 자동으로 압축 파일을 해제하여 이 문제를 해결합니다. 워크플로우는 다음 작업을 수행합니다:

1. Unity WebGL 빌드 실행
2. 생성된 `.gz` 파일 압축 해제
3. `index.html`에서 `.gz` 확장자 참조 제거
4. GitHub Pages에 배포

**로컬 빌드 시**: 수동으로 빌드한 경우, 다음 명령어로 압축을 해제할 수 있습니다:

```bash
cd build/webgl/Build
gunzip -k build.data.gz
gunzip -k build.framework.js.gz
gunzip -k build.wasm.gz
```

그리고 `build/webgl/index.html`에서 `.gz` 확장자를 제거하여 압축 해제된 파일을 참조하도록 수정해야 합니다.

## 📝 라이센스

이 프로젝트는 테스트 목적으로 만들어졌습니다.
