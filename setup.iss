; CostBIM Standalone 정식 윈도우 인스톨러 패키징 스크립트
; 제작: Lead Engineer Agent (v3.0)

[Setup]
AppName=CostBIM Standalone
AppVersion=1.5.0
AppPublisher=CostBIM team
DefaultDirName={pf}\CostBIM
DefaultGroupName=CostBIM
OutputDir=C:\Users\LYH\Desktop
OutputBaseFilename=CostBIM_Setup
SetupIconFile=D:\CostBim\costbim.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
DisableWelcomePage=no
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\CostBIM.Standalone.exe

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Files]
Source: "D:\CostBim\bin\x64\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\CostBIM Standalone"; Filename: "{app}\CostBIM.Standalone.exe"; IconFilename: "{app}\CostBIM.Standalone.exe"; IconIndex: 0
Name: "{userdesktop}\CostBIM Standalone"; Filename: "{app}\CostBIM.Standalone.exe"; IconFilename: "{app}\CostBIM.Standalone.exe"; IconIndex: 0; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "바탕 화면에 바로가기 만들기"; GroupDescription: "추가 작업:"

[Run]
Filename: "{app}\CostBIM.Standalone.exe"; Description: "CostBIM Standalone 실행하기"; Flags: nowait postinstall skipifsilent
