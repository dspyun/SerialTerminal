
![image](https://github.com/user-attachments/assets/9807b764-3947-4022-8858-f4b71bdacc56)


Serial Terminal
241031
1. Ansi Color 중에 white와 red 컬러 적용
2. port 선택하면 open 버튼을 누르지 않고 바로 연결되도록 변경
3. 

20241021
1. split conationer를 적용하여 log와 summary의 창이 변화해도 비율을 유지하도록 변경
2. gps 위도,경도, 리셋정보 추출 기능 추가

20241018
1. 실행아이콘을 윈도우 디폴트에서 커스텀으로 변경
2. 레이아웃을 ankor방식에서 dock방식으로 변경하여 윈도우 사이즈 변경해도 richtextbox가 따로 놀지 않게 수정
   
20241017
1. ANSI Coloring 기능은 적용되지 않았다(추가필요)
2. 두 개의 richtextbox를 사용하였다. 위는 단순 모니터링용, 아래는 GPS 정보 요약용이다
3. 아래는 nrf9160 gps로그에서 prn, c/n0를 모아 놓기만 한 것으로 gps 계측기 없이 감도를 간이로 측정하기 위한 것이다
4. richtextbox가 자동크기 조절이 안된다 따라서 창의 크기를 변화시키면 richtextbox의 영역이 겹칠 수도 있다(수정필요)

