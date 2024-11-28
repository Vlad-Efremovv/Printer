# Printer

 Запускается на порту **http://localhost:8080/**

## Проект
 [Uploading Printer.zip…]()


## [Route("printPDF")]
Post -> Печать PDF файла 

## Принимает параметры:
  - file
  - tpsIP          string
  - [ 9100 ] port        int 
  - [ 1 ] zoomImage      float
  - [ true ] inversion   bool ( Конвертация цветов )
 

![image](https://github.com/user-attachments/assets/d11f1f1d-a544-4741-ac14-2646fca9cfc0)

## Настройка моей печати для документа : [АРМ работника станка.pdf](https://github.com/user-attachments/files/17949651/default.pdf)

  - file
  - tpsIP     - 192.168.0.113        
  - port      - 9100      
  - zoomImage - 2.5       
  - inversion - true
    
![photo_2024-11-28_18-01-37](https://github.com/user-attachments/assets/3fc97ed4-eda3-4336-99dc-65a9d78afc12)

