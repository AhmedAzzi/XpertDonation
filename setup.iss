[Setup]
AppName=XpertDonation
AppVersion=1.0
AppPublisher=XpertPharm
DefaultDirName={pf}\XpertDonation
DefaultGroupName=XpertDonation
OutputDir=installer
OutputBaseFilename=XpertDonation_Setup
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
SetupIconFile=

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\XpertDonation"; Filename: "{app}\XpertPharm5Donation.exe"
Name: "{group}\Uninstall XpertDonation"; Filename: "{uninstallexe}"
Name: "{commondesktop}\XpertDonation"; Filename: "{app}\XpertPharm5Donation.exe"

[Run]
Filename: "{app}\XpertPharm5Donation.exe"; Description: "Launch XpertDonation"; Flags: nowait postinstall skipifsilent
