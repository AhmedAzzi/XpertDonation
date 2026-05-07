[Setup]
AppName=XDonation
AppVersion=1.0
AppPublisher=Ahmed Azzi
AppPublisherURL=mailto:mrahmedazzi@gmail.com
DefaultDirName={pf}\XDonation
DefaultGroupName=XDonation
OutputDir=installer
OutputBaseFilename=XDonation_Setup
Compression=lzma
SolidCompression=yes
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
