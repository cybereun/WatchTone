param([string]$SdkPath=$env:ANDROID_HOME)
$ErrorActionPreference='Stop'
if(!$SdkPath){
    $line=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'local.properties') -ErrorAction SilentlyContinue | Where-Object {$_ -like 'sdk.dir=*'} | Select-Object -First 1
    if($line){$SdkPath=$line.Substring(8).Replace('\:',':').Replace('\\','\')}
}
if(!$SdkPath){throw 'Set ANDROID_HOME or pass -SdkPath.'}
$env:ANDROID_HOME=$SdkPath
$keyDir=Join-Path $env:LOCALAPPDATA 'LebiStudio\WatchTone\signing'
$keyFile=Join-Path $keyDir 'watchtone.p12'
$secretFile=Join-Path $keyDir 'password.dpapi'
$release=Join-Path $PSScriptRoot 'release'
New-Item -ItemType Directory -Force -Path $keyDir,$release | Out-Null
if((Test-Path $keyFile) -and !(Test-Path $secretFile)){throw 'Existing signing key needs its original password.'}
if(!(Test-Path $secretFile)){
    $bytes=New-Object byte[] 32
    $rng=[Security.Cryptography.RandomNumberGenerator]::Create()
    try{$rng.GetBytes($bytes)}finally{$rng.Dispose()}
    ConvertTo-SecureString ([Convert]::ToBase64String($bytes)) -AsPlainText -Force | ConvertFrom-SecureString | Set-Content -LiteralPath $secretFile
}
$secure=Get-Content $secretFile | ConvertTo-SecureString
$env:WATCHTONE_SIGN_PASSWORD=[Net.NetworkCredential]::new('',$secure).Password
Push-Location $PSScriptRoot
try {
    if(!(Test-Path $keyFile)){
        & keytool -genkeypair -alias watchtone -keyalg RSA -keysize 3072 -validity 10000 -storetype PKCS12 -keystore $keyFile -storepass:env WATCHTONE_SIGN_PASSWORD -dname 'CN=WatchTone, O=Lebi Studio, C=KR'
        if($LASTEXITCODE){throw 'Signing key generation failed'}
    }
    & .\gradlew.bat assemblePhoneRelease assembleWatchRelease --console=plain
    if($LASTEXITCODE){throw 'APK build failed'}
    $signer=Join-Path $SdkPath 'build-tools\35.0.0\apksigner.bat'
    foreach($device in 'phone','watch'){
        $unsigned=Join-Path $PSScriptRoot "app\build\outputs\apk\$device\release\app-$device-release-unsigned.apk"
        $target=Join-Path $release "WatchTone-$device-1.0.2.apk"
        & $signer sign --ks $keyFile --ks-key-alias watchtone --ks-pass env:WATCHTONE_SIGN_PASSWORD --v4-signing-enabled false --out $target $unsigned
        if($LASTEXITCODE){throw "Signing failed for $device"}
        & $signer verify --verbose --print-certs $target
        if($LASTEXITCODE){throw "Signature verification failed for $device"}
    }
} finally {Pop-Location; Remove-Item Env:\WATCHTONE_SIGN_PASSWORD -ErrorAction SilentlyContinue}