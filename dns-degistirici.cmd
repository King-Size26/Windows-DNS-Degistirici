@echo off
setlocal enabledelayedexpansion
title DNS Degistirme Araci
chcp 65001 >nul
color 0A

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ========================================
    echo  [HATA] Yonetici yetkisi gerekli!
    echo  Bu dosyaya sag tiklayip "Yonetici olarak calistir" deyin.
    echo ========================================
    pause
    exit /b
)

:menu
cls
echo ========================================
echo        DNS AYARLARI DEGISTIRME ARACI
echo ========================================
echo.
echo  1 - Manuel DNS      (127.0.2.2  / 127.0.2.3)
echo  2 - Quad9 DNS       (9.9.9.10   / 149.112.112.10)
echo  3 - Google DNS      (8.8.8.8    / 8.8.4.4)
echo  4 - Cloudflare DNS  (1.1.1.1    / 1.0.0.1)
echo  5 - Otomatik (DHCP)
echo  6 - DNS Cache Temizle (Flush)
echo  7 - Cikis
echo.
set /p secim="Seciminizi yapin (1-7): "

if "%secim%"=="1" goto manual
if "%secim%"=="2" goto quad9
if "%secim%"=="3" goto google
if "%secim%"=="4" goto cloudflare
if "%secim%"=="5" goto auto
if "%secim%"=="6" goto flush
if "%secim%"=="7" goto exit
echo Gecersiz secim! Lutfen 1-7 arasi bir sayi girin.
timeout /t 2 >nul
goto menu

:manual
cls
echo Manuel DNS'e Geciriliyor... 127.0.2.2 / 127.0.2.3
call :applyDns "127.0.2.2" "127.0.2.3" "Manuel"
goto menu

:quad9
cls
echo Quad9 DNS'e Geciriliyor... 9.9.9.10 / 149.112.112.10
call :applyDns "9.9.9.10" "149.112.112.10" "Quad9"
goto menu

:google
cls
echo Google DNS'e Geciriliyor... 8.8.8.8 / 8.8.4.4
call :applyDns "8.8.8.8" "8.8.4.4" "Google"
goto menu

:cloudflare
cls
echo Cloudflare DNS'e Geciriliyor... 1.1.1.1 / 1.0.0.1
call :applyDns "1.1.1.1" "1.0.0.1" "Cloudflare"
goto menu

:: Ortak DNS uygulama rutini - %1=primary %2=secondary %3=label
:applyDns
set "pDns=%~1"
set "sDns=%~2"
set "label=%~3"
echo Aktif adaptorler araniyor...
set "found=0"
for /f "delims=" %%a in ('powershell -NoProfile -Command "Get-NetAdapter | Where-Object {$_.Status -eq 'Up'} | Select-Object -ExpandProperty Name"') do (
    set /a found+=1
    echo.
    echo --- Adaptor: "%%a" ---
    netsh interface ip set dns "%%a" static !pDns!
    if !errorlevel! neq 0 (
        echo [HATA] "%%a" birincil DNS ayarlanamadi! Kod: !errorlevel!
    ) else (
        netsh interface ip add dns "%%a" !sDns! index=2
        if !errorlevel! neq 0 echo [UYARI] "%%a" yedek DNS eklenemedi! Kod: !errorlevel!
        echo [OK] "%%a" !label! DNS'e guncellendi.
    )
)
if !found!==0 (
    echo [BILGI] Powershell adaptor bulamadi, netsh fallback deneniyor...
    for /f "tokens=1,2,3,* delims= " %%a in ('netsh interface show interface ^| findstr /R "Connected"') do (
        if not "%%d"=="" (
            set /a found+=1
            echo.
            echo --- Adaptor: "%%d" ---
            netsh interface ip set dns "%%d" static !pDns!
            if !errorlevel! neq 0 (
                echo [HATA] "%%d" DNS ayarlanamadi!
            ) else (
                netsh interface ip add dns "%%d" !sDns! index=2
                echo [OK] "%%d" !label! DNS'e guncellendi.
            )
        )
    )
)
if !found!==0 (
    echo [HATA] Hic aktif adaptor bulunamadi!
    pause
    goto :eof
)
echo.
echo Toplam !found! adaptor !label! DNS'e guncellendi.
echo DNS Cache temizleniyor...
ipconfig /flushdns
echo Islem tamamlandi!
pause
goto :eof

:auto
cls
echo DNS Otomatik Al'a Geciriliyor (DHCP)...
echo.
echo Aktif adaptorler araniyor...
set "found=0"
for /f "delims=" %%a in ('powershell -NoProfile -Command "Get-NetAdapter | Where-Object {$_.Status -eq 'Up'} | Select-Object -ExpandProperty Name"') do (
    set /a found+=1
    echo.
    echo --- Adaptor: "%%a" ---
    netsh interface ip set dns "%%a" dhcp
    if !errorlevel! neq 0 (
        echo [HATA] "%%a" DHCP moduna gecemedi! Kod: !errorlevel!
    ) else (
        echo [OK] "%%a" DHCP moduna alindi.
    )
)
if %found%==0 (
    echo [BILGI] Powershell adaptor bulamadi, netsh fallback deneniyor...
    for /f "tokens=1,2,3,* delims= " %%a in ('netsh interface show interface ^| findstr /R "Connected"') do (
        if not "%%d"=="" (
            set /a found+=1
            echo.
            echo --- Adaptor: "%%d" ---
            netsh interface ip set dns "%%d" dhcp
            if !errorlevel! neq 0 (
                echo [HATA] "%%d" DHCP moduna gecemedi!
            ) else (
                echo [OK] "%%d" DHCP moduna alindi.
            )
        )
    )
)
if %found%==0 (
    echo [HATA] Hic aktif adaptor bulunamadi!
    pause
    goto menu
)
echo.
echo Toplam %found% adaptor DHCP moduna alindi.
echo DNS Cache temizleniyor...
ipconfig /flushdns
echo Islem tamamlandi!
pause
goto menu

:flush
cls
echo DNS Cache Temizleniyor...
ipconfig /flushdns
echo.
if %errorlevel% equ 0 (
    echo DNS Cache basariyla temizlendi!
) else (
    echo [HATA] Cache temizlenemedi! Yonetici olarak calistirdiginizdan emin olun.
)
pause
goto menu

:exit
cls
echo Program kapatiliyor...
timeout /t 1 >nul
exit /b
