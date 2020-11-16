using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Video2Gba
{
    public class ROM
    {
        public static readonly StringBuilder baseAssembly = new StringBuilder(@".gba
.arm
.open ""test.gba"", 0x8000000

.definelabel DMA0SAD, 0x040000D4
.definelabel DMA0DAD, 0x040000D8
.definelabel DMA0CNT_L, 0x040000Dc
.definelabel REG_DISPLAYCONTROL, 0x04000000
.definelabel VIDEOMODE_3, 0x0003
.definelabel BGMODE_2, 0x0400
.definelabel FrameCounter, 0x02026000

.org 0x8000000
//Hop to main
b armMain
.align 4
.org 0x80000C0
armMain:
ldr r0, = MainFunc + 1
//hop to thumb main
bx r0
interruptHandler:
LDR R3, = 0x4000202
MOV R2, #1
STRH R2, [R3]
LDR     R2, = 0x3007FF8
LDRH R3, [R2]
MOV    R1, #1
ORR R3, R1
STRH    R3, [R2]
BX      LR
.thumb

VBlankIntrWait:
SWI             5
BX LR

// void __fastcall DMA3(int srcAdd, int dstAdd, int size)
DMA3:
                LDR R3, = 0x40000D4
                STR R0, [R3]
                LDR     R3, = 0x40000D8
                STR R1, [R3]
                MOV    R3, #0x80000000
                ORR R3, R2
                LDR     R2, = 0x40000DC
                STR R3, [R2]

                nop

                nop

                nop
                BX      LR

// void __fastcall LZ77UnCompVram(int src, int dst)

LZ77UnCompVram:
                // src = R0                               
                // dst = R1                               
                SWI     0x12
                BX LR


                //decode, framedata
decode:
                add R3, R0,#4                           
                LDR R2, [R3]
                LDR     R1, = 0x88FFFF00
                CMP R2, R1
                BEQ     rawCopy
                LDR     R3, = 0x88FFFF01
                CMP R2, R3
                BEQ     lzUncomp

returnDecode:                          
                BX LR
rawCopy:                               


                LDR R2, [R3,#4]                              
                ADD    R3, #8
                LDR     R1, =0x40000D4
                STR     R3, [R1]
                LDR     R3, = 0x40000D8
                LDR R1, =#0x6000000
                STR     R1, [R3]
                LDR   R3,= #0x80000000
                ORR    R3, R2
                LDR     R2, = 0x40000DC
                STR R3, [R2]
                B       returnDecode

lzUncomp:             
LDR R1, =#0x6000000  
mov r2, 0xC           
add R0, R0, r2
                SWI     0x12
                B returnDecode

.pool
.align 4
MainFunc:
    //Set up bg
    ldr r0, =#0x4000000
	//videomode3|bgmode_2
	bl Setup
    ldr r1,= #0x403
    str r1, [r0]
    ldr r2, =#FrameCounter//FrameCounter offset is in r2
	mov r0, 0
    str r0, [r2]

ourLoop:

//Clear registers
mov r0, 0
mov r1, 0
mov r2, 0
mov r3, 0

    CheckMaxFrames:
    //loop while FrameCounter < MaxFrames
    LoadFrameCounter:

        ldr r3,= #FrameCounter
		ldr r2, [r3]
        LoadMaxFrames:	

        ldr r1, =#MaxFrames
		ldr r1, [r1]
        CompareFrameValues:

        cmp r2, r1//if r0 is greater, then we're done
        bge end
        //otherwise loop.
        //increment frame count
        IncrementFrameCounter:

        add r2, 1
        str r2, [r3]
        mov r5, r2
        //get gfx table
        IndexFrameTable:	

        ldr r1, =#FrameTable
		lsl r2, 2

        add r1, r2//r1 is a struct of {offset, size} both words/long		

        ldr r1, [r1]

        mov r2, r1	//mov r1 into r2

    CalcSize:		

        add r2, 4
        ldr r2, [r2]//r2 contains size offset
	DecodeStuff:
        ldr r0, [r1]
        bl VBlankIntrWait
        bl decode
    b ourLoop
end:
    ldr r2, =#FrameCounter//FrameCounter offset is in r2
	mov r0, 0

    str r0, [r2]
b ourLoop
//pop {r14}
//bx r0
.pool

.align
DivArm2:                                 ; CODE XREF: Divide+12↓p

.definelabel denominator, -8
.definelabel numerator,-4

                PUSH    {R7,LR
    }
    SUB SP, SP, #8
                ADD     R7, SP, #0
                STR R0, [R7,#8+numerator]
                STR     R1, [R7,#8+denominator]
                SWI     7
                MOV    R3, #0
                MOV    R0, R3
                MOV     SP, R7
                ADD     SP, SP, #8
                POP     {R7}
POP
{ R1}
BX R1
; End of function DivArm2


; =============== S U B R O U T I N E =======================================
.align 
Divide:                                  ; CODE XREF: Setup + A↓p
                                          ; play_sound + E8↓p...

                PUSH
{ R7,LR}
SUB SP, SP, #8
                ADD     R7, SP, #0
                STR     R0, [R7,#8+numerator]
                STR     R1, [R7,#8+denominator]
                LDR     R2, [R7,#8+numerator]
                LDR     R3, [R7,#8+denominator]
                MOV    R1, R2          ; denominator
                MOV    R0, R3          ; numerator
                BL      DivArm2
                MOV    R3, R0
                MOV    R0, R3
                MOV     SP, R7
                ADD     SP, SP, #8
                POP     {R7}
                POP
{ R1}
BX R1
.align 2
.arm
.align 4
on_vblank:                               ; DATA XREF: Setup + 22↓o
                                          ; .text: off_1B0↓o

   .definelabel temp, -2

                PUSH    {R7, LR}
                SUB SP, SP, #8
                ADD     R7, SP, #0
                LDR     R3, = interrupt_enable
                LDR     R3, [R3]
                MOV    R2, #0
                STRH    R2, [R3]
                LDR     R3, = interrupt_state
                LDR     R2, [R3]
                ADD    R3, R7, #6
                LDRH    R2, [R2]
                STRH    R2, [R3]
                LDR     R3, = interrupt_state
                LDR     R3, [R3]
                LDRH    R3, [R3]
                LSL    R3, R3, #0x10
                LSR    R3, R3, #0x10
                MOV    R2, R3
                MOV    R3, #1
                AND    R3, R2
                CMP     R3, #1
                BNE     loc_E2
                LDR     R3, = _channel_a_vblanks_remaining
                LDR     R3, [R3]
                LDR     R3, [R3]
                CMP     R3, #0
                BNE     loc_9E
                LDR     R3, = _channel_a_total_vblanks
                LDR     R2, [R3]
                LDR     R3, = _channel_a_vblanks_remaining
                LDR     R3, [R3]
                LDR     R2, [R2]
                STR     R2, [R3]
                LDR     R3, = dma1_control
                LDR     R3, [R3]
                MOV    R2, #0
                STR     R2, [R3]
                LDR     R3, = _soundPnter
                LDR     R2, [R3]
                LDR     R3, = dma1_source
                LDR     R3, [R3]
                LDR     R2, [R2]
                STR     R2, [R3]
                LDR     R3, = dma1_control
                LDR     R3, [R3]
                LDR     R2, = 0xB6400000
                STR     R2, [R3]
                B       loc_AA
; ---------------------------------------------------------------------------

loc_9E:                                  ; CODE XREF: on_vblank + 34↑j
                  LDR     R3, = _channel_a_vblanks_remaining
                LDR     R3, [R3]
                SUB    R1, R3, #4
                LDR     R2, = _channel_a_vblanks_remaining
                STR     R1, [R2]
                LDR     R3, [R3]

loc_AA:                                 ; CODE XREF: on_vblank + 5E↑j
                  LDR     R3, = _channel_b_vblanks_remaining
                LDR     R3, [R3]
                LDR     R3, [R3]
                CMP     R3, #0
                BNE     loc_D6
                LDR     R3, = sound_control
                LDR     R3, [R3]
                LDRH    R3, [R3]
                LSL    R3, R3, #0x10
                LSR    R2, R3, #0x10
                LDR     R3, = sound_control
                LDR     R3, [R3]
                LDR     R1, = 0x4FFF
                AND    R2, R1
                LSL    R2, R2, #0x10
                LSR    R2, R2, #0x10
                STRH    R2, [R3]
                LDR     R3, = dma2_control
                LDR     R3, [R3]
                MOV    R2, #0
                STR     R2, [R3]
                B       loc_E2
; ---------------------------------------------------------------------------

loc_D6:                                  ; CODE XREF: on_vblank + 74↑j
                  LDR     R3, = _channel_b_vblanks_remaining
                LDR     R3, [R3]
                SUB     R1, R3, #4
                LDR     R2, = _channel_b_vblanks_remaining
                STR     R1, [R2]
                LDR     R3, [R3]

loc_E2:                                  ; CODE XREF: on_vblank + 2A↑j
                                          ; on_vblank + 96↑j
                    LDR     R3, = interrupt_state
                LDR     R3, [R3]
                ADD    R2, R7, #6
                LDRH    R2, [R2]
                STRH    R2, [R3]
                LDR     R3, = interrupt_enable
                LDR     R3, [R3]
                MOV    R2, #1
                STRH    R2, [R3]
                NOP
                MOV     SP, R7
                ADD     SP, SP, #8
                POP     {R7}
                POP
{ R0}
BX R0

Setup:
                PUSH
{ R7,LR}
ADD R7, SP, #0
                LDR     R3, = 0x448E6
                MOV    R1, R3; denominator
      MOV    R0, #1          ; numerator
                BL     Divide
                MOV    R2, R0
                LDR     R3, = _sampleRate
                LDR     R3, [R3]
                STR     R2, [R3]
                LDR     R3, = interrupt_enable
                LDR     R3, [R3]
                MOV    R2, #0
                STRH    R2, [R3]
                LDR     R3, = interrupt_callback
                LDR     R3, [R3]
                LDR     R2, = (on_vblank)
      STR     R2, [R3]
                LDR     R3, = interrupt_selection
                LDR     R3, [R3]
                LDRH    R3, [R3]
                LSL    R3, R3, #0x10
                LSR    R2, R3, #0x10
                LDR     R3, = interrupt_selection
                LDR     R3, [R3]
                MOV    R1, #1
                ORR    R2, R1
                LSL    R2, R2, #0x10
                LSR    R2, R2, #0x10
                STRH    R2, [R3]
                LDR     R3, = display_interrupts
                LDR     R3, [R3]
                LDRH    R3, [R3]
                LSL    R3, R3, #0x10
                LSR    R2, R3, #0x10
                LDR     R3, = display_interrupts
                LDR     R3, [R3]
                MOV    R1, #8
                ORR    R2, R1
                LSL    R2, R2, #0x10
                LSR    R2, R2, #0x10
                STRH    R2, [R3]
                LDR     R3, = interrupt_enable
                LDR     R3, [R3]
                MOV    R2, #1
                STRH    R2, [R3]
                LDR     R3, = sound_control
                LDR     R3, [R3]
                MOV    R2, #0
                STRH    R2, [R3]
                NOP
                MOV     SP, R7
                POP     {R7}
                POP
{ R0}
BX R0

.thumb
play_sound:

.definelabel channel,-0x15
.definelabel sample_rate, -0x14
.definelabel total_samples, -0x10
.definelabel sound, -0xC
.definelabel rem,-8
.definelabel ticks_per_sample, -2


PUSH    { R7,LR}
SUB SP, SP, #0x18
                ADD     R7, SP, #0
                STR     R0, [R7,#0x18+sound]
                STR     R1, [R7,#0x18+total_samples]
                STR     R2, [R7,#0x18+sample_rate]
                MOV    R2, R3
                ADD    R3, R7, #3
                STRB    R2, [R3]
LDR R3, = _soundPnter
                LDR     R3, [R3]
                LDR     R2, [R7,#0x18+sound]
                STR     R2, [R3]
LDR R3, = timer0_control
                LDR     R3, [R3]
                MOV    R2, #0
                STRH    R2, [R3]
                ADD    R3, R7, #3
                LDRB    R3, [R3]
                CMP     R3, #0x41 ; 'A'
                BNE     loc_1F4
                LDR     R3, = dma1_control
                LDR     R3, [R3]
                MOV    R2, #0
                STR     R2, [R3]
                B       loc_204
; ---------------------------------------------------------------------------

loc_1F4:                                 ; CODE XREF: play_sound + 28↑j
                  ADD    R3, R7, #3
                LDRB    R3, [R3]
                CMP     R3, #0x42 ; 'B'
                BNE     loc_204
                LDR     R3, = dma2_control
                LDR     R3, [R3]
                MOV    R2, #0
                STR     R2, [R3]

loc_204:                                 ; CODE XREF: play_sound + 32↑j
                                          ; play_sound + 3A↑j
                    ADD    R3, R7, #3
                LDRB    R3, [R3]
                CMP     R3, #0x41 ; 'A'
                BNE     loc_228
                LDR     R3, = sound_control
                LDR     R3, [R3]
                LDRH    R3, [R3]
                LSL    R3, R3, #0x10
                LSR    R2, R3, #0x10
                LDR     R3, = sound_control
                LDR     R3, [R3]
                LDR    R1, =#0xB00
                ORR    R2, R1
                LSL    R2, R2, #0x10
                LSR    R2, R2, #0x10
                STRH    R2, [R3]
                B       loc_248
; ---------------------------------------------------------------------------

loc_228:                                 ; CODE XREF: play_sound + 4A↑j
                  ADD    R3, R7, #3
                LDRB    R3, [R3]
                CMP     R3, #0x42 ; 'B'
                BNE     loc_248
                LDR     R3, = sound_control
                LDR     R3, [R3]
                LDRH    R3, [R3]
                LSL    R3, R3, #0x10
                LSR    R2, R3, #0x10
                LDR     R3, = sound_control
                LDR     R3, [R3]
                LDR     R1, = 0xFFFFB000
                ORR    R2, R1
                LSL    R2, R2, #0x10
                LSR    R2, R2, #0x10
                STRH    R2, [R3]

loc_248:                                ; CODE XREF: play_sound + 66↑j
                                          ; play_sound + 6E↑j
                    LDR     R3, = master_sound
                LDR     R3, [R3]
                MOV    R2, #0x80
                STRH    R2, [R3]
                ADD    R3, R7, #3
                LDRB    R3, [R3]
                CMP     R3, #0x41 ; 'A'
                BNE     loc_278
                LDR     R3, = _soundPnter
                LDR     R2, [R3]
                LDR     R3, = dma1_source
                LDR     R3, [R3]
                LDR     R2, [R2]
                STR     R2, [R3]
                LDR     R3, = fifo_buffer_a
                LDR     R2, [R3]
                LDR     R3, = dma1_destination
                LDR     R3, [R3]
                STR     R2, [R3]
                LDR     R3, = dma1_control
                LDR     R3, [R3]
                LDR     R2, = 0xB6400000
                STR     R2, [R3]
                B       loc_29E
; ---------------------------------------------------------------------------

loc_278:                                 ; CODE XREF: play_sound + 96↑j
                  ADD    R3, R7, #3
                LDRB    R3, [R3]
                CMP     R3, #0x42 ; 'B'
                BNE     loc_29E
                LDR     R3, = _soundPnter
                LDR     R2, [R3]
                LDR     R3, = dma2_source
                LDR     R3, [R3]
                LDR     R2, [R2]
                STR     R2, [R3]
                LDR     R3, = fifo_buffer_b
                LDR     R2, [R3]
                LDR     R3, = dma2_destination
                LDR     R3, [R3]
                STR     R2, [R3]
                LDR     R3, = dma2_control
                LDR     R3, [R3]
                LDR     R2, = 0xB6400000
                STR     R2, [R3]

loc_29E:                                 ; CODE XREF: play_sound + B6↑j
                                          ; play_sound + BE↑j
                    LDR     R2, [R7,#0x18+sample_rate]
                LDR   R3,=#0x1000000
                MOV    R1, R2          ; denominator
                MOV    R0, R3; numerator
      BL      Divide
                MOV    R2, R0
                MOV    R1, #0x16
                ADD    R3, R7, R1
                STRH    R2, [R3]
                LDR     R3, = timer0_data
                LDR     R3, [R3]
                ADD    R2, R7, R1
                LDRH    R2, [R2]
                NEG    R2, R2
                LSL    R2, R2, #0x10
                LSR    R2, R2, #0x10
                STRH    R2, [R3]
                ADD    R3, R7, R1
                LDRH    R3, [R3]
                LDR     R2, [R7,#0x18+total_samples]
                MUL    R3, R2

      MOV    R2, R3

      LDR     R3, = _sampleRate

      LDR     R3, [R3]
LDR R3, [R3]
                MUL     R3, R2
                STR     R3, [R7,#0x18+rem]
                ADD     R3, R7, #3
                LDRB    R3, [R3]
CMP R3, #0x41 ; 'A'
                BNE     loc_2F6
                LDR     R3, = _channel_a_vblanks_remaining
                LDR     R3, [R3]
                LDR     R2, [R7,#0x18+rem]
                STR     R2, [R3]
LDR R3, = _channel_a_vblanks_remaining
                LDR     R2, [R3]
                LDR     R3, = _channel_a_total_vblanks
                LDR     R3, [R3]
                LDR     R2, [R2]
                STR     R2, [R3]
                B       loc_306
; ---------------------------------------------------------------------------

loc_2F6:                                ; CODE XREF: play_sound + 11E↑j
                  ADD    R3, R7, #3
                LDRB    R3, [R3]
                CMP     R3, #0x42 ; 'B'
                BNE     loc_306
                LDR     R3, = _channel_b_vblanks_remaining
                LDR     R3, [R3]
                LDR     R2, [R7,#0x18+rem]
                STR     R2, [R3]

loc_306:; CODE XREF: play_sound + 134↑j
     ; play_sound + 13C↑j
LDR     R3, = timer0_control
                LDR     R3, [R3]
                MOV    R2, #0x80
                STRH    R2, [R3]
                NOP
                MOV     SP, R7
                ADD     SP, SP, #0x18
                POP     {R7}
                POP
{ R0}
BX R0
; End of function play_sound

.pool
.align 4
; 
.definelabel dma1_source, 0x40000BC           ; DATA XREF: on_vblank + 4E↑o
                                       ; on_vblank + 50↑r...
              
; volatile unsigned int* dma1_destination
.definelabel dma1_destination, 0x40000C0          ; DATA XREF: play_sound + A8↑o
                                          ; play_sound + AA↑r...
                
; volatile unsigned int* dma1_control
.definelabel dma1_control , 0x40000C4           ; DATA XREF: on_vblank + 42↑o
                                          ; on_vblank + 44↑r...
              
; volatile unsigned int* dma2_source
.definelabel dma2_source , 0x40000C8           ; DATA XREF: play_sound + C4↑o
                                          ; play_sound + C6↑r...
              
; volatile unsigned int* dma2_destination
.definelabel dma2_destination ,0x40000CC          ; DATA XREF: play_sound + D0↑o
                                          ; play_sound + D2↑r...
              
; volatile unsigned int* dma2_control
.definelabel dma2_control  , 0x40000D0           ; DATA XREF: on_vblank + 8E↑o
                                          ; on_vblank + 90↑r...
               
; volatile unsigned int* dma3_source
.definelabel dma3_source   , 0x40000D4


; volatile unsigned int* dma3_destination
.definelabel dma3_destination , 0x40000D8


; volatile unsigned int* dma3_control
.definelabel dma3_control   , 0x40000DC


; volatile unsigned __int16 *master_sound
.definelabel master_sound , 0x4000084           ; DATA XREF: play_sound: loc_248↑o
                                         ; play_sound + 8A↑r...
              
; volatile unsigned __int16 *sound_control
.definelabel  sound_control, 0x4000082           ; DATA XREF: on_vblank + 76↑o
                                          ; on_vblank + 78↑r...
              
; volatile unsigned __int16 *interrupt_enable
.definelabel interrupt_enable , 0x4000208          ; DATA XREF: on_vblank + 6↑o
                                          ; on_vblank + 8↑r...
               
; volatile unsigned __int16 *interrupt_selection
.definelabel interrupt_selection, 0x4000200       ; DATA XREF: Setup + 26↑o
                                          ; Setup + 28↑r...
              
; volatile unsigned __int16 *interrupt_state
.definelabel interrupt_state,0x4000202           ; DATA XREF: on_vblank + E↑o
                                          ; on_vblank + 10↑r...
               
; volatile unsigned int* interrupt_callback
.definelabel interrupt_callback , 0x3007FFC        ; DATA XREF: Setup + 1E↑o
                                          ; Setup + 20↑r...
            
; volatile unsigned __int16 *display_interrupts
.definelabel display_interrupts, 0x4000004        ; DATA XREF: Setup + 3E↑o
                                          ; Setup + 40↑r...
              
; volatile unsigned int* channel_a_vblanks_remaining
.definelabel _channel_a_vblanks_remaining, 0x2026010 ; DATA XREF: on_vblank + 2C↑o
                                          ; on_vblank + 2E↑r...
           
; volatile unsigned int* channel_a_total_vblanks
.definelabel _channel_a_total_vblanks , 0x2026020  ; DATA XREF: on_vblank + 36↑o
                                          ; on_vblank + 38↑r...
               
; volatile unsigned int* channel_b_vblanks_remaining
.definelabel _channel_b_vblanks_remaining ,0x2026030
                                        ; DATA XREF: on_vblank: loc_AA↑o
                                         ; on_vblank + 6E↑r...
             
; volatile unsigned int* soundPnter
.definelabel _soundPnter , 0x2026040           ; DATA XREF: on_vblank + 4A↑o
                                          ; on_vblank + 4C↑r...
           
; volatile unsigned int* sampleRate
.definelabel _sampleRate , 0x2026050           ; DATA XREF: Setup + 10↑o
                                          ; Setup + 12↑r...

; volatile unsigned __int8 *fifo_buffer_a
.definelabel fifo_buffer_a,0x40000A0           ; DATA XREF: play_sound + A4↑o
                                          ; play_sound + A6↑r...
              
; volatile unsigned __int8 *fifo_buffer_b
.definelabel fifo_buffer_b, 0x40000A4           ; DATA XREF: play_sound + CC↑o
                                          ; play_sound + CE↑r...
             
; volatile unsigned __int16 *timer0_data
.definelabel timer0_data,0x4000100           ; DATA XREF: play_sound + F4↑o
                                          ; play_sound + F6↑r...
             
; volatile unsigned __int16 *timer0_control
.definelabel timer0_control , 0x4000102           ; DATA XREF: play_sound + 1A↑o


");

    }
}
