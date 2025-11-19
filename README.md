# Sample for Linux drm 

Change <code>start.sh</code> to start your app

User: <code>kiosk</code>

Service: <code>[Kiosk.service](./Linux/Kiosk.service)</code>

Linux settings: 

```
sudo apt install dotnet-sdk-8.0 alsa-base alsa-utils p7zip-full plymouth-themes udisks2 libdrm2 libgbm1 mesa-utils libfontconfig1 libfreetype6 libharfbuzz0b libssl-dev libinput10
```

```
sudo usermod -a -G input,video username
```
