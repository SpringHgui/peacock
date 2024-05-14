CHCP 65001
@echo off
color 0e
@echo ==================================
@echo 提醒：请右键本文件，用管理员方式打开。
@echo ==================================
@echo Start Install BXScheduler.Master

sc create BXScheduler.Master binPath="%~dp0Scheduler.Master.exe" start=auto 
sc description BXScheduler.Master "分布式任务调度平台"
Net Start BXScheduler.Master
pause