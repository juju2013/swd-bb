\ SWD-BB
\   bitbang swd protocol for NRF52
\   (C) Copyright 2018 by juju2013@github
\   under BSD 2-Clause License, see LICENSE

\ This implement ( a subset of ) the ARM Cortex's SWD protocol
\   using big-banging on a stm32f103 ( bluepill ) board.
\ The final word Factory is used to do a mass_erase on a nRF52832 chip

\ Requires some files from jeelabs' embello libraries 
\ https://github.com/jeelabs/embello :
\  clock.fs
\  hexdump.fs
\  io.fs
\  pins64.fs

compiletoram

\ --- PIN definitions
PB13 constant DAT   \ DATA pin
PB14 constant CLK   \ CLOCK pin
PB15 constant RST   \ RESET pin (for DEBUG only, not part of SWD protocol)
PB9  constant DBG   \ DEBUG pin (for DEBUG only, not part of SWD protocol)

0   constant AP
1   constant DAP
500 constant sw.delay \ adjust this for fewer/faster or more/slower protocole speed

\ SWD request constants
0      constant DP-DAP
6 bit  constant DP-AP
0      constant DP-WRITE
5 bit  constant DP-READ

\ Global parity calculation
0 variable parity

: parity? ( -- c ) \ return 0 or 1
 parity @ 1 and
;

: >parity ( u --  ) \ Add 1 bit to parity
 0 bit and parity @ + parity !
;

: c>parity ( u --  ) \ Add 1 byte to parity
 0 parity !
 8 0 do dup i bit and if 1 >parity then loop
 drop parity? 
;

: .revadr ( addr -- rdda ) \ inverse address bits in swd request
  dup %01 = if %10 else
  dup %10 = if %01 else
  dup
  then then
  swap drop
;

: dpreq ( DAP/AP READ/XRITE Addr -- u ) \ caculate dp request value
  .revadr 3 lshift swap + swap + dup c>parity 2 lshift + 7 bit + 1+
;

: sw.half ( -- )  \ half-cycle timing delay for SW
  sw.DELAY 0 do loop inline 
;
  
: tic
  sw.half CLK ios! inline
  \ begin CLK io@ until inline \ clock stretching to finish
;

: tac CLK ioc! sw.half inline 
;

: b>sw ( f -- )  \ send one SWD bit
  0<> DAT io! tic sw.half tac 
;

: sw>b ( -- f )  \ receive one swd bit
  tic  sw.half tac DAT io@ \ latch at failling edge
;

: idle 0 b>sw ;

: >sw ( c -- ) \ send a byte to swd (MSB)
  RST iox! RST iox!
  DAT ios!
  0 7 do dup i bit and b>sw -1 +loop
  drop
;

: trn   sw.half tic sw.half tac ; \ turnover

: epilog ( -- ) \ finishing transaction
  OMODE-pp DAT IO-MODE! 
  8 0 do idle loop   
; 

: sw? ( f -- ) \ print sw response status
       dup %100 and if ." FAULT"
  else dup %010 and if ." WAIT"
  else dup %001 and if ." OK"
  else dup             ." ???" dup binary . decimal
  then then then 
  drop
;

: ack? ( -- u ) \ read ack response
  DBG iox! 
  IMODE-FLOAT DAT IO-MODE!
  DBG iox! 
  0 3 0 do sw>b if i bit or then loop \ ack/nak
;

: 32sw> ( -- u ) \ read 32 bit LSB data
  0 parity !
  0 32 0 do sw>b dup >parity if i bit or then loop \ result
  sw>b drop \ ignore parity error
;

: sw.get ( reg -- ack val ) \ read SW register
  >sw ack? 
  dup %001 = if
    32sw> 
  else
    $DEADBEEF
  then
  epilog
;


: >sw32 ( u -- ) \ write 32 bit LSB data
  0 parity !
  32 0 do dup i bit and if 1 else 0 then dup >parity b>sw loop
  parity? b>sw
  epilog drop
;

: sw.set ( val reg -- ack ) \ write value to register
  >sw ack? dup %001 = if
    trn trn OMODE-pp DAT IO-MODE! 
    swap dup ." sw.set " hex. ."  " DBG iox! >sw32
  then
  epilog
;

\ Mimics some JLINK commands
: dap.get ( reg -- val flag )
  DP-READ DP-DAP rot dpreq sw.get
;

: ap.get ( reg -- val flag )
  DP-READ DP-AP rot dpreq sw.get
;

: dap.set ( val reg -- flag )
  DP-DAP DP-WRITE rot dpreq sw.set 
;

: ap.set ( val reg -- flag )
  DP-AP DP-WRITE rot dpreq sw.set 
;

: sw.reset   DAT ios! 50 0 do tic sw.half tac loop idle idle ;

: swd-init
  OMODE-FAST DAT io-mode!
  OMODE-FAST CLK io-mode!
  OMODE-FAST DBG io-mode!
  OMODE-pp DAT IO-MODE!
  OMODE-pp CLK IO-MODE!
  OMODE-pp RST IO-MODE!
  OMODE-pp DBG IO-MODE!
  
  sw.reset $79 >sw $E7 >sw sw.reset
  8 0 do idle loop
  10 ms
  DP-DAP DP-READ 0 dpreq sw.get hex. sw?
  cr
;

: init
  swd-init 10 ms
  RST iox!
  cr 
  ." CTL/STAT=" %01 dap.get hex. sw? 
  10 ms cr 
;

\ do a mass_erase, clear the protection bits and ALL flash/ram contents
: Factory
  RST iox!
  ." write DP 1 $50000000="
  $50000000 1 dap.set sw? cr
  ." write DP 2 $01000000="
  $01000000 2 dap.set sw? cr
  ." write AP 1 $00000001="
  $00000001 1 ap.set sw? cr

  100 ms

  ." CTRL-AP="
  2 dap.get hex. sw? cr
  
  ." CTRL-AP2="
  2 dap.get hex. sw? cr

  ." write AP 0 0$01"
  1 0 ap.set sw? cr
  
  ." write AP 0 0$00"
  0 0 ap.set sw? cr

  ." write AP 1 0$00"
  0 1 ap.set sw? cr

  ." APPROTECT="
  3 ap.get hex. sw? cr
  
  ." APPROTECT2="
  3 ap.get hex. sw? cr

;

\ to do a mass_erase, un comment next line
\ init Factory
