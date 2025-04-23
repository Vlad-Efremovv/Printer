# Printer

 Запускается на порту **http://localhost:8080/**

## [Route("printPDF")] - чековый принтер
## Принимает параметры:
  - file
  - tpsIP          string
  - [ 9100 ] port        int 
  - [ 1 ] zoomImage      float
  - [ true ] inversion   bool ( Конвертация цветов )

![image](https://github.com/user-attachments/assets/d11f1f1d-a544-4741-ac14-2646fca9cfc0)


## [Route("printSticker")] - Этикеточный принтер
## Принимает параметры:
  - tpsIP			string
  - secretQR		string
  - descriptionQR	string? (перечисление через запятую)
  - [ 14 ] textSize	int 
  - [ 14 ] sizeQR	int 
  - [ 9100 ] port	float
 
![image](https://github.com/user-attachments/assets/b749fc44-e4f3-42c8-932c-8fe369053b9b)
