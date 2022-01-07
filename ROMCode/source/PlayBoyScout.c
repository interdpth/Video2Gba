/* sound.c
 * displays the sound playing capapbilities of the GBA */

#define PL_MPEG_IMPLEMENTATION
#include "pl_mpeg.h"
#include "Alietest.h"
#include <stdio.h>
 /*#include "background.h"

 #include "zelda_music_16K_mono.h"
 #include "zelda_treasure_16K_mono.h"
 #include "zelda_secret_16K_mono.h"
 */
int ticks;
int GetTicks(){return ticks;}
 /* the display control pointer points to the gba graphics register */
volatile unsigned long* display_control = (volatile unsigned long*)0x4000000;
#define MODE0 0x00
#define BG0_ENABLE 0x100
#define BG1_ENABLE 0x200
#define BG2_ENABLE 0x400
#define BG3_ENABLE 0x800

/* the button register holds the bits which indicate whether each button has
 * been pressed - this has got to be volatile as well
 */
volatile unsigned short* buttons = (volatile unsigned short*)0x04000130;

/* the bit positions indicate each button - the first bit is for A, second for
 * B, and so on, each constant below can be ANDED into the register to get the
 * status of any one button */
#define BUTTON_A (1 << 0)
#define BUTTON_B (1 << 1)
#define BUTTON_SELECT (1 << 2)
#define BUTTON_START (1 << 3)
#define BUTTON_RIGHT (1 << 4)
#define BUTTON_LEFT (1 << 5)
#define BUTTON_UP (1 << 6)
#define BUTTON_DOWN (1 << 7)
#define BUTTON_R (1 << 8)
#define BUTTON_L (1 << 9)

 /* the control registers for the four tile layers */
volatile unsigned short* bg0_control = (volatile unsigned short*)0x4000008;
volatile unsigned short* bg1_control = (volatile unsigned short*)0x400000a;
volatile unsigned short* bg2_control = (volatile unsigned short*)0x400000c;
volatile unsigned short* bg3_control = (volatile unsigned short*)0x400000e;

/* palette is always 256 colors */
#define PALETTE_SIZE 256

/* the address of the color palette */
volatile unsigned short* bg_palette = (volatile unsigned short*)0x5000000;

/* define the timer control registers */
volatile unsigned short* timer0_data = (volatile unsigned short*)0x4000100;
volatile unsigned short* timer0_control = (volatile unsigned short*)0x4000102;

/* make defines for the bit positions of the control register */
#define TIMER_FREQ_1 0x0
#define TIMER_FREQ_64 0x2
#define TIMER_FREQ_256 0x3
#define TIMER_FREQ_1024 0x4
#define TIMER_ENABLE 0x80

/* the GBA clock speed is fixed at this rate */
#define CLOCK 16777216 
#define CYCLES_PER_BLANK 280896

/* turn DMA on for different sizes */
#define DMA_ENABLE 0x80000000
#define DMA_16 0x00000000
#define DMA_32 0x04000000

/* this causes the DMA destination to be the same each time rather than increment */
#define DMA_DEST_FIXED 0x400000

/* this causes the DMA to repeat the transfer automatically on some interval */
#define DMA_REPEAT 0x2000000

/* this causes the DMA repeat interval to be synced with timer 0 */
#define DMA_SYNC_TO_TIMER 0x30000000

/* pointers to the DMA source/dest locations and control registers */
volatile unsigned int* dma1_source = (volatile unsigned int*)0x40000BC;
volatile unsigned int* dma1_destination = (volatile unsigned int*)0x40000C0;
volatile unsigned int* dma1_control = (volatile unsigned int*)0x40000C4;

volatile unsigned int* dma2_source = (volatile unsigned int*)0x40000C8;
volatile unsigned int* dma2_destination = (volatile unsigned int*)0x40000CC;
volatile unsigned int* dma2_control = (volatile unsigned int*)0x40000D0;

volatile unsigned int* dma3_source = (volatile unsigned int*)0x40000D4;
volatile unsigned int* dma3_destination = (volatile unsigned int*)0x40000D8;
volatile unsigned int* dma3_control = (volatile unsigned int*)0x40000DC;

const uint RAWHEADER = 0x88FFFF00;
const uint LZCOMPRESSEDHEADER = 0x88FFFF01;
const uint INTERLACERLEHEADER = 0x88FFFF22;
const uint INTERLACERLEHEADER2 = 0x88FFFF23;
const uint DESCRIBEHEADER = 0x88FFFF03;
const uint LUTHEADER = 0x88FFFF02;
const uint DIFFHEADER = 0x88FFFF12;
const uint QUADDIFFHEADER = 0x88FFFF13;
const uint QUADDIFFHEADER2 = 0x88FFFF14;
const uint RLEHEADER = 0x88FFFF15;
const uint NINTYRLHEADERINTR = 0x88FFFF76;
typedef struct
{
	unsigned long thesize;
	unsigned long headervalue;
	unsigned long size;
	unsigned char* source;
}CompFrame;

//Frames don't have a type.
typedef struct
{
	unsigned long* address;
	unsigned long size;
}VidFrame;


plm_packet_t test;
/* copy data using DMA channel 3 (normal memory transfers) */
void memcpy16_dma(unsigned short* dest, unsigned short* source, int amount) {
	*dma3_source = (unsigned int)source;
	*dma3_destination = (unsigned int)dest;
	*dma3_control = DMA_ENABLE | DMA_16 | amount;
}

/* this function checks whether a particular button has been pressed */
unsigned char button_pressed(unsigned short button) {
	/* and the button register with the button constant we want */
	unsigned short pressed = *buttons & button;

	/* if this value is zero, then it's not pressed */
	if (pressed == 0) {
		return 1;
	}
	else {
		return 0;
	}
}

/* return a pointer to one of the 4 character blocks (0-3) */
volatile unsigned short* char_block(unsigned long block) {
	/* they are each 16K big */
	return (volatile unsigned short*)(0x6000000 + (block * 0x4000));
}

/* return a pointer to one of the 32 screen blocks (0-31) */
volatile unsigned short* screen_block(unsigned long block) {
	/* they are each 2K big */
	return (volatile unsigned short*)(0x6000000 + (block * 0x800));
}

/* the global interrupt enable register */
volatile unsigned short* interrupt_enable = (unsigned short*)0x4000208;

/* this register stores the individual interrupts we want */
volatile unsigned short* interrupt_selection = (unsigned short*)0x4000200;

/* this registers stores which interrupts if any occured */
volatile unsigned short* REG_IF = (unsigned short*)0x4000202;

/* the address of the function to call when an interrupt occurs */
volatile unsigned int* interrupt_callback = (unsigned int*)0x3007FFC;

/* this register needs a bit set to tell the hardware to send the vblank interrupt */
volatile unsigned short* display_interrupts = (unsigned short*)0x4000004;

/* the interrupts are identified by number, we only care about this one */
#define INTERRUPT_VBLANK 0x1

/* allows turning on and off sound for the GBA altogether */
volatile unsigned short* master_sound = (volatile unsigned short*)0x4000084;
#define SOUND_MASTER_ENABLE 0x80

/* has various bits for controlling the direct sound channels */
volatile unsigned short* sound_control = (volatile unsigned short*)0x4000082;

/* bit patterns for the sound control register */
#define SOUND_A_RIGHT_CHANNEL 0x100
#define SOUND_A_LEFT_CHANNEL 0x200
#define SOUND_A_FIFO_RESET 0x800
#define SOUND_B_RIGHT_CHANNEL 0x1000
#define SOUND_B_LEFT_CHANNEL 0x2000
#define SOUND_B_FIFO_RESET 0x8000

/* the location of where sound samples are placed for each channel */
volatile unsigned char* fifo_buffer_a = (volatile unsigned char*)0x40000A0;
volatile unsigned char* fifo_buffer_b = (volatile unsigned char*)0x40000A4;

/* global variables to keep track of how much longer the sounds are to play */
unsigned int channel_a_vblanks_remaining = 0;
unsigned int channel_a_total_vblanks = 0;
unsigned int channel_b_vblanks_remaining = 0;
#define INT_VBLANK 	0x0001
#define INT_HBLANK 	0x0002
#define INT_VCOUNT 	0x0004
#define INT_TIMER0 	0x0008
#define INT_TIMER1 	0x0010
#define INT_TIMER2 	0x0020
#define INT_TIMER3 	0x0040
#define INT_COM 	0x0080
#define INT_DMA0 	0x0100
#define INT_DMA1	0x0200
#define INT_DMA2 	0x0400
#define INT_DMA3 	0x0800
#define INT_BUTTON 	0x1000
#define INT_CART 	0x2000

/* play a sound with a number of samples, and sample rate on one channel 'A' or 'B' */
void play_sound(const signed char* sound, int total_samples, int sample_rate, char channel) {
	/* start by disabling the timer and dma controller (to reset a previous sound) */
	*timer0_control = 0;
	if (channel == 'A') {
		*dma1_control = 0;
	}
	else if (channel == 'B') {
		*dma2_control = 0;
	}

	/* output to both sides and reset the FIFO */
	if (channel == 'A') {
		*sound_control |= SOUND_A_RIGHT_CHANNEL | SOUND_A_LEFT_CHANNEL | SOUND_A_FIFO_RESET;
	}
	else if (channel == 'B') {
		*sound_control |= SOUND_B_RIGHT_CHANNEL | SOUND_B_LEFT_CHANNEL | SOUND_B_FIFO_RESET;
	}

	/* enable all sound */
	*master_sound = SOUND_MASTER_ENABLE;

	/* set the dma channel to transfer from the sound array to the sound buffer */
	if (channel == 'A') {
		*dma1_source = (unsigned int)sound;
		*dma1_destination = (unsigned int)fifo_buffer_a;
		*dma1_control = DMA_DEST_FIXED | DMA_REPEAT | DMA_32 | DMA_SYNC_TO_TIMER | DMA_ENABLE;
	}
	else if (channel == 'B') {
		*dma2_source = (unsigned int)sound;
		*dma2_destination = (unsigned int)fifo_buffer_b;
		*dma2_control = DMA_DEST_FIXED | DMA_REPEAT | DMA_32 | DMA_SYNC_TO_TIMER | DMA_ENABLE;
	}

	/* set the timer so that it increments once each time a sample is due
	 * we divide the clock (ticks/second) by the sample rate (samples/second)
	 * to get the number of ticks/samples */
	unsigned short ticks_per_sample = CLOCK / sample_rate;

	/* the timers all count up to 65536 and overflow at that point, so we count up to that
	 * now the timer will trigger each time we need a sample, and cause DMA to give it one! */
	*timer0_data = 65536 - ticks_per_sample;

	/* determine length of playback in vblanks
	 * this is the total number of samples, times the number of clock ticks per sample,
	 * divided by the number of machine cycles per vblank (a constant) */
	if (channel == 'A') {
		channel_a_vblanks_remaining = (total_samples * ticks_per_sample) / CYCLES_PER_BLANK;
		channel_a_total_vblanks = channel_a_vblanks_remaining;
	}
	else if (channel == 'B') {
		channel_b_vblanks_remaining = (total_samples * ticks_per_sample) / CYCLES_PER_BLANK;
	}

	/* enable the timer */
	*timer0_control = TIMER_ENABLE | TIMER_FREQ_1;
}

#define ARM __attribute__((__target__("arm")))
#define THUMB __attribute__((__target__("thumb")))
#define REG_IFBIOS (*(unsigned short*)(0x3007FF8))

//indicates if framebufer can be used as a buffer or not.
int canDmaImage;
int vblankcounter = 0;
/* this function is called each vblank to get the timing of sounds right */
ARM void on_vblank() {

	/* disable interrupts for now and save current state of interrupt */
	*interrupt_enable = 0;
	unsigned short temp = *REG_IF;

	/* look for vertical refresh */
	if ((*REG_IF & INTERRUPT_VBLANK) == INTERRUPT_VBLANK) {

		/* update channel A */
		if (channel_a_vblanks_remaining == 0) {
			/* restart the sound again when it runs out */
			channel_a_vblanks_remaining = channel_a_total_vblanks;
			*dma1_control = 0;
	//		*dma1_source = (unsigned int)alie;
			*dma1_control = DMA_DEST_FIXED | DMA_REPEAT | DMA_32 |
				DMA_SYNC_TO_TIMER | DMA_ENABLE;
		}
		else {
			channel_a_vblanks_remaining--;
		}

		/* update channel B */
		if (channel_b_vblanks_remaining == 0) {
			/* disable the sound and DMA transfer on channel B */
			*sound_control &= ~(SOUND_B_RIGHT_CHANNEL | SOUND_B_LEFT_CHANNEL | SOUND_B_FIFO_RESET);
			*dma2_control = 0;
		}
		else {
			channel_b_vblanks_remaining--;
		}

	  //	memcpy16_dma(0x6000000, 0x2002000, 240 * 160);
	}
	vblankcounter++;
	/* restore/enable interrupts */
	*REG_IF = temp;
	REG_IFBIOS |= 1;
	*interrupt_enable = 1;

}
void* workArea=0x2002000;
void* workArea2=0x2002000 + ( 240 * 160 *2) + 0x200;
void Setup()
{
canDmaImage=1;
	/* create custom interrupt handler for vblank - whole point is to turn off sound at right time
	   * we disable interrupts while changing them, to avoid breaking things */
	*interrupt_enable = 0;
	*interrupt_callback = (unsigned int)&on_vblank;
	*interrupt_selection |= INTERRUPT_VBLANK;
	*display_interrupts |= 9;//;
	*interrupt_enable = 1;

	/* clear the sound control initially */
	*sound_control = 0;
}
//extern VidFrame theframes[];
int FrameCounter;
char CanDraw;

void VBlankIntrWait()
{

	asm("swi 0x05");
ticks++;
}
int  Lz77Uncomp(int src, int dst)
{
	asm("swi 0x12"); ;
}
int  RLUncomp(int src, int dst)
{
	asm("swi 0x14"); ;
}
/*
The length must be a multiple of 4 bytes (32bit mode) or 2 bytes (16bit mode). The (half)wordcount in r2 must be length/4 (32bit mode) or length/2 (16bit mode), ie. length in word/halfword units rather than byte units.

  r0    Source address        (must be aligned by 4 for 32bit, by 2 for 16bit)
  r1    Destination address   (must be aligned by 4 for 32bit, by 2 for 16bit)
  r2    Length/Mode
          Bit 0-20  Wordcount (for 32bit), or Halfwordcount (for 16bit)
          Bit 24    Fixed Source Address (0=Copy, 1=Fill by {HALF}WORD[r0])
          Bit 26    Datasize (0=16bit, 1=32bit)

*/
int _CpuSet(int  src, int  dst, int size)
{
	asm("swi 0xb"); ;
}


void Exception(int errorcode, char* msg)
{
    _CpuSet(msg, 0x3000000, 128);
	while (1);

}

unsigned short Read16(unsigned char* src)
{
	//return (src[0] << 8) | src[1];
	return *(unsigned short*)src;
}
unsigned long Read32(unsigned char* src)
{
	//return (src[0] << 24) | (src[1] << 16) | (src[2] << 8) | src[3];
	return *(unsigned long*)src;
}


//pls implement cpuset as cpufastset requires divisible by 32
const int EndOfFile = 0x00464F45;
const int EndOfFile2 = 0x00454F46;
const int CompHeader = 0x504d4f43;
//Calls cpuset using appropriate flags.
int CpuSet(unsigned char*  src, unsigned char*  dst, int size, char fill, char isu32)
{
	int fillFlag =  (fill & 1) << 24;
    int u32Flag = (isu32 & 1) << 26;
    int maxLen=(1<<20);
    for(int s = 0; s<size-1;)
	{        
        int CopySize = size > maxLen ? size-maxLen : size;
        s+=CopySize;
		_CpuSet(src, dst, (CopySize/(2 + isu32*2)) | fillFlag | u32Flag);
	}
}


int FillByCharCpu(unsigned char val, int* dst, int size)
{
	int test = 0;
	//make a word from our val lol
	ushort hey[2] = {val, val};

    CpuSet(&hey, dst, size, 1,0);	
}

void Fill(unsigned char* ptr, unsigned char val, unsigned long size)
{
	if(size>8)
	{      
		FillByCharCpu(val, ptr, size);
		return;
	}
	for (register int c = 0; c < size;c++) *(ptr++) = val;
}

void Copy(unsigned char* src, unsigned char* dst, unsigned long size)
{    
	if(size>8)
	{      
        if(size % 4 ==0)
        {
			CpuSet(src, dst, size,0,1);
			return;
        }
		CpuSet(src, dst, size,0,0);
		return;
	}

	for (register int c = 0; c < size;c++) *(dst++) = *(src++);
}



//two buffers 
//__attribute__((section(".iwram"), target("arm"), noinline)) 
THUMB rgb24torgb16()
{
	unsigned char* src = workArea;
	unsigned short* dst = workArea;

		for(int x =0; x<240; x++)
		{
			for(int y=0;y<160;y++)
			{
				unsigned char* srcp = src[(y*240+x)*3];//R G B format
				char r = srcp[0];
				char g = srcp[1];		
				char b = srcp[2];
				dst[(y*240+x)]= (((r >> 3) & 31) | (((g >> 3) & 31) << 5) | (((b >> 3) & 31) << 10));
			}
		}
		
				
		
	
	
}

//__attribute__((section(".iwram"), target("arm"), noinline))  
THUMB void app_on_video(plm_t *mpeg, plm_frame_t *frame) {

		int stride =  frame->width * 3;
		plm_frame_to_rgb(frame, workArea, stride);
		while(1);
		//rgb24torgb16();//a0
		VBlankIntrWait();
		CanDraw=1;
	
}
const int FrameCount;
const int FPS;
plm_t *plm;

int main() {
	char wants_to_quit = 0;
		double last_time=0;
	ticks=0;
	//plm_set_audio_enabled(FALSE);
		// Initialize plmpeg, load the video file, install decode callbacks
	plm = plm_create_with_memory((uint8_t *)ALIEtest, ALIEtest_size, 0);
	
	int b=	plm_get_framerate(plm);
	int k= 0xDEAD1;
	int c=	plm_get_samplerate(plm);
	 k= 0xDEAD2;
	int d=	plm_get_duration(plm);
	 k= 0xDEAD3;
	if (!plm) {
		*(unsigned long*)0x3000000 = 0xFFDDEE22;
		while(1);
	}
	//set up screen
	(*(unsigned short*)0x4000000) = 0x403;
	Setup();
    *(unsigned long*)0x6000000 = 0xFFAA;
	//Start "naturally"
	FrameCounter = FrameCount;
	//fps
	int delay = Div(60, FPS);
plm_set_video_decode_callback(plm, app_on_video, NULL);

	
	plm_set_loop(plm, TRUE);
	while (!wants_to_quit)
	{
		double seek_to = -1;
		if (vblankcounter % delay == 0) {
			CanDraw = 1;
		}
		if (FrameCounter >= FrameCount)
		{
			FrameCounter = 0;
			
		}
	// Compute the delta time since the last app_update(), limit max step to 
	// 1/30th of a second
	double current_time = (double)GetTicks()/1000;
	double elapsed_time = current_time - last_time;
	if (elapsed_time > 1.0 / 10.0) {
		elapsed_time = 1.0 / 10.0;
	}
	last_time = current_time;

	// Seek or advance decode
	if (seek_to != -1) {
	//	SDL_ClearQueuedAudio(self->audio_device);
		plm_seek(plm, seek_to, FALSE);
	}
	else {
		plm_decode(plm, elapsed_time);
	}

	if (plm_has_ended(plm)) {
		wants_to_quit = 1;
	}
		if (CanDraw) {

		//	*(unsigned long*)0x3000000 = 0xFFDDEE33;
		//while(1);
			CanDraw = 0;
		}

		VBlankIntrWait();
		
	}
	return 0;
}