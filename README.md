# Приложение для парсинга прокси для телеграмм на Windows
Данная версия опирается на данные с .txt файлов https://github.com/kort0881/telegram-proxy-collector<br> 
За основу взяты [proxy_ru.txt](https://raw.githubusercontent.com/kort0881/telegram-proxy-collector/main/proxy_ru.txt) и [proxy_eu.txt](https://raw.githubusercontent.com/kort0881/telegram-proxy-collector/main/proxy_eu.txt)<br> 
Большой список прокси предоставлен @Surfboardv2ray<br> 
С версии 1.7 добавлена возможность подгрузить свой список из .txt<br> 
# Что умеет?<br>
Проксирование параллельными потоками (5-20 за раз), выдача готового результата с сортировкой по пингу (ограничения задаются в настройках)<br> 
Программа проверяет доступность и пинг до самих серверов. Однако, работоспособность всех серверов в качестве прокси для telegram не гарантируется, как и их  блокировка.<br> 
Это - версия для Windows. <b>Версия для Android <a href="https://github.com/ComradeBingo/Proxy-Telegram-Android">лежит здесь</a></b> <br> 
Для работы с Windows 7 [нужен фикс](#Windows-7-fix) поддержки TlS 1.2/1.3  

<div align="center"><img width="720" height="497" align="center" src="https://github.com/user-attachments/assets/58ad7389-5f0e-4cfe-99f5-28216df1ee71" /></div><br>

## Windows 7 fix 
1. Открыть блокнот, вставить представленный ниже код, сохранить файл с раширение .reg
2. Запустить файл, согласиться на добавление записей в реестр, перезагрузить ПК

```
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.1\Client]
"DisabledByDefault"=dword:00000000

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.1\Server]
"DisabledByDefault"=dword:00000000

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Client]
"DisabledByDefault"=dword:00000000

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Server]
"DisabledByDefault"=dword:00000000
```
