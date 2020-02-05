.code
LightenASM PROC
	MOVDQU XMM0, [RCX]			; za�adowanie 16 pikseli do rejestru XMM0
	MOVQ XMM2, RDX				; za�adowanie warto�ci zmiany jasno�ci do rejestru xmm 2
	MOV EAX, 0					; za�adowanie 0 do eax (licznik petli)
	L1:							; petla zapelniajaca kazdy bajt xmm1 wartoscia zmiany jasnosci 
		PADDB XMM1, XMM2		; dodanie xmm2 na ostatnie 8 bitow xmm1
		PSLLDQ XMM1, 1			; przesuniecie rejestru xmm1 bitowe o 1 bajt w lewo
		ADD EAX, 1				; inkrementacja licznika petli
		CMP EAX, 16				; sprawdzanie warunku koncowego petli (16 iteracji - rejestr xmm1 - 128 bitow = 16 bajtow)	
		JL L1					; powrot do poczatka petli
	PADDB XMM1, XMM2 			; ostatnie dodanie xmm2 do xmm1
	PADDUSB XMM0, XMM1			; zsumowanie rejestru zawieraj�cego piksele z rejestrem zawieraj�cym warto�ci zmiany jasno�ci 
	MOVDQU [RCX], XMM0			; za�adowanie nowych warto�ci pikseli z powrotem do tablicy 	
	RET							
LightenASM ENDP

DimASM PROC
	MOVDQU XMM0, [RCX]			; za�adowanie 16 pikseli do rejestru XMM0
	MOVQ XMM2, RDX				; za�adowanie warto�ci zmiany jasno�ci do rejestru xmm 2
	MOV EAX, 0					; za�adowanie 0 do eax (licznik petli)
	L1:							; petla zapelniajaca kazdy bajt xmm1 wartoscia zmiany jasnosci 
		PADDB XMM1, XMM2		; dodanie xmm2 na ostatnie 8 bitow xmm1
		PSLLDQ XMM1, 1			; przesuniecie rejestru xmm1 bitowe o 1 bajt w lewo
		ADD EAX, 1				; inkrementacja licznika petli
		CMP EAX, 16				; sprawdzanie warunku koncowego petli (16 iteracji - rejestr xmm1 - 128 bitow = 16 bajtow)	
		JL L1					; powrot do poczatka petli
	PADDB XMM1, XMM2 			; ostatnie dodanie xmm2 do xmm1
	PSUBUSB XMM0, XMM1			; odejmowanie rejestru zawieraj�cego piksele z rejestrem zawieraj�cym warto�ci zmiany jasno�ci 
	MOVDQU [RCX], XMM0			; za�adowanie nowych warto�ci pikseli z powrotem do tablicy 	
	RET							
DimASM ENDP
END
