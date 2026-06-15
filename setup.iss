[Setup]
AppName=XDonation
AppVersion=1.1
AppPublisher=Ahmed Azzi
AppPublisherURL=mailto:mrahmedazzi@gmail.com
DefaultDirName={pf}\XDonation
DefaultGroupName=XDonation
OutputDir=installer
AppVerName=XDonation 1.1
OutputBaseFilename=XDonation_Setup_v1.1
Compression=lzma2/ultra64
SolidCompression=no
SignedUninstaller=no
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
SetupIconFile=Resources\xdonation.ico
AppCopyright=Copyright © 2026 Ahmed Azzi

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\XDonation"; Filename: "{app}\XDonation.exe"; IconFilename: "{app}\XDonation.exe"
Name: "{group}\Uninstall XDonation"; Filename: "{uninstallexe}"
Name: "{commondesktop}\XDonation"; Filename: "{app}\XDonation.exe"; IconFilename: "{app}\XDonation.exe"

[Run]
Filename: "{app}\XDonation.exe"; Description: "Launch XDonation"; Flags: nowait postinstall skipifsilent
