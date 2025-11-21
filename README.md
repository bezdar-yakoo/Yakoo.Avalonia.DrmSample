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

Отключить ожидание сети + настроить 20-ю подсеть
-----------------------------------------------------------------

### 1.1. Отключить ожидание сети навсегда

```bash
sudo systemctl mask systemd-networkd-wait-online.service
sudo systemctl daemon-reload
```

Готово — при следующей загрузке ждать сеть не будет.  
(Если вдруг передумай — `sudo systemctl unmask ...`.)

* * *

### 1.2. Настроить подсеть 192.168.20.0/24 на том же интерфейсе

Предположим, интерфейс — `enp1s0` (как у тебя в `50-cloud-init.yaml`).

1. Открыть netplan-конфиг:
    
    ```bash
    sudo nano /etc/netplan/50-cloud-init.yaml
    ```
    
2. Сделать так (добавили `renderer` и статический адрес):
    
    ```yaml
    network:
      version: 2
      renderer: networkd
      ethernets:
        enp1s0:
          dhcp4: true              # берём IP от роутера (интернет)
          addresses:
            - 192.168.20.1/24      # доп. IP для подсети 20
          optional: true           # чтобы не ждать линк/адрес
    ```
    
3. Применить:
    
    ```bash
    sudo netplan generate
    sudo netplan apply
    ```
    
4. Проверить:
    
    ```bash
    ip a show enp1s0
    ```
    

Должно быть **два IPv4-адреса**: один от DHCP и `192.168.20.1/24`.  
Устройство в подсети ставишь, например, на `192.168.20.2/24`, и пингуешь.
