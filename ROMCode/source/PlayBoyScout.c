/* sound.c
 * displays the sound playing capapbilities of the GBA */

#include <stdio.h>
 /*#include "background.h"

 #include "zelda_music_16K_mono.h"
 #include "zelda_treasure_16K_mono.h"
 #include "zelda_secret_16K_mono.h"
 */

#include "muta.h"

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
			*dma1_source = (unsigned int)muta;
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

	  if(canDmaImage)	memcpy16_dma(0x6000000, 0x2002000, 240 * 160);
	}
	vblankcounter++;
	/* restore/enable interrupts */
	*REG_IF = temp;
	REG_IFBIOS |= 1;
	*interrupt_enable = 1;

}
void* workArea=0x2002000;
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
extern VidFrame theframes[];
int FrameCounter;
char CanDraw;

void VBlankIntrWait()
{

	asm("swi 0x05");

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

void UncompIPSRLE(unsigned char* src, unsigned char* dst)
{
	if (dst == 0)
	{
		Exception(0,  "DESTINATION IS 0");
	}
	register unsigned char* patch = src;
	register int header = Read32(patch); patch += 4;
	if (header != CompHeader) Exception(0,  "HEADER DOES NOT MATCH");

	register int offset = Read32(patch); patch += 4;
	register ushort size = 0;
	register int s = 0;
	while (offset != EndOfFile)
	{
		size = Read16(patch); patch += 2;
		unsigned char* target = (unsigned char*)&dst[offset];
		// If RLE patch.
		if (size == 0x6969)
		{
			size = Read16(patch); patch += 2;
			unsigned char val = *patch; patch++;
			Fill(target, val, size);
		}
		// If normal patch.
		else
		{
			Copy(patch, target, size);
			patch += size;
		}
		offset = Read32(patch); patch += 4;
	}
}

signed int GbatroidDecomp(int size, unsigned char *src, unsigned char *dst)
{
  signed int maxSize; // r5
  int encodedSizeCheck; // r5
  unsigned char *nextSrc; // r1
  unsigned char *nextDst; // r2
  unsigned char *dstAddr; // r6
  signed int srccnt; // r4
  int curVal; // r0
  unsigned char *nextAddr; // r1
  int compCheck; // r3
  int i; // r3
  int bit16Val; // r3
  unsigned char *nextByte; // r1
  int bit16Check; // r3
  int j; // r3
  int v17; // r3
 unsigned char *v18; // r1

  size = size;
  maxSize = 0;
  if ( size )
  {
    if ( size != 1 )
    {
      *dst = *src;
      nextSrc = src + 1;
      nextDst = dst + 1;
      *nextDst++ = 0;
      *nextDst = *nextSrc;
      src = nextSrc + 1;
      *++nextDst = 0;
      dst = nextDst + 1;
    }
  }
  else
  {
    encodedSizeCheck = *src++;
    if ( encodedSizeCheck )
    {
      if ( encodedSizeCheck != 1 && encodedSizeCheck != 2 )
      {
        maxSize = 0x2000;
      }
      else
      {
        maxSize = 0x1000;
      }
    }
    else
    {
      maxSize = 0x800;
    }
  }
  dstAddr = dst;
  srccnt = 0;
  do
  {
    curVal = *src;
    nextAddr = src + 1;
    if ( curVal == 1 )
    {
      compCheck = *nextAddr;
      src = nextAddr + 1;
      ++srccnt;
      for ( ; compCheck; ++src )
      {
        if ( compCheck & 0x80 )
        {
          for ( i = compCheck & 0x7F; i; --i )
          {
            *dst = *src;
            dst += 2;
          }
          ++src;
        }
        else
        {
          while ( compCheck )
          {
            *dst = *src++;
            dst += 2;
            --compCheck;
          }
        }
        compCheck = *src;
      }
    }
    else
    {
      bit16Val = *nextAddr;
      nextByte = nextAddr + 1;
      bit16Check = (bit16Val << 8) | *nextByte;
      src = nextByte + 1;
      ++srccnt;
      for ( ; bit16Check; src = v18 + 1 )
      {
        if ( bit16Check & 0x8000 )
        {
          for ( j = bit16Check & 0x7FFF; j; --j )
          {
            *dst = *src;
            dst += 2;
          }
          ++src;
        }
        else
        {
          while ( bit16Check )
          {
            *dst = *src++;
            dst += 2;
            --bit16Check;
          }
        }
        v17 = *src;
        v18 = src + 1;
        bit16Check = (v17 << 8) | *v18;
      }
    }
    dst = dstAddr + 1;
  }
  while ( srccnt <= 1 );
  return maxSize;
}
typedef struct { unsigned long cmpsize[4]; unsigned long decmpsize[4]; unsigned char* pnt; } help;

typedef struct { unsigned long cmpsize[4];  unsigned char* pnt; } help2;

void FrameCompareCompQuad(unsigned char* src, unsigned char* dst)
{
	unsigned char* tgt = dst;
	//src will point to size table, then serialized array
	int compBufferIndex = 0;//pointer inside buffer

	help* k = (src);

	unsigned char* compBufferStart = &k->pnt;

	for (int i = 0;i < 4;i++) 
    {
		UncompIPSRLE(compBufferStart, tgt);
		compBufferStart += k->cmpsize[i];
		tgt += k->decmpsize[i];
	}
}

void FrameCompareCompQuadnintyo(unsigned char* src, unsigned char* dst)
{
	unsigned char* tgt = dst;
	//src will point to size table, then serialized array
	int compBufferIndex = 0;//pointer inside buffer

	help2* k = (src);

	unsigned char* compBufferStart = &k->pnt;

	for (int i = 0;i < 4;i++) 
    {
		int sz = RLUncomp(compBufferStart, tgt);
		compBufferStart += k->cmpsize[i];
		tgt += sz;
	}
}
void ApplyDifferences(unsigned char* src, unsigned char* dst)
{
   unsigned char* p = src;
   int count=*p;p+=4;
   int* offsets = *p; p+=4*count; 
   unsigned char* data = p;
   for(int i=0;i<count;i++)
   {
        int byteCount = *(unsigned long*)data; data+=4;
//(unsigned char*  src, unsigned char*  dst, int size, char fill, char isu32)
        CpuSet(data, dst[offsets[i]], byteCount,0,0);

   }


  

}


void FrameCompare(unsigned char* src, unsigned char* dst)
{
	unsigned char* tgt = dst;
unsigned char* patch = src;
	//src will point to size table, then serialized array
	int compBufferIndex = 0;//pointer inside buffer

//	help2* k = (src);2

	
    unsigned short arrayCount = *(unsigned short*)patch; patch+=2;
    unsigned char* compressTable = (unsigned char*)patch;patch+=1*arrayCount;
    unsigned long* compressSizes=(unsigned long*)patch; patch+=4*arrayCount;
unsigned char* compBufferStart=patch;
	for (int i = 0;i < arrayCount;i++) 
    {
		int diffOffset=compBufferStart;
		int sz=0;
        switch(compressTable[i])
        {
         
         case 1:
			diffOffset=workArea;
           sz=Lz77Uncomp(compBufferStart, diffOffset);
                 break;
         case 2:
			diffOffset=workArea;
			sz = RLUncomp(compBufferStart, diffOffset);		
                 break;
        }      



        ApplyDifferences(diffOffset, tgt);
	
		compBufferStart += compressSizes[i];
		tgt += sz;
	}
}

const uint NINTYRLHEADER = 0x88FFFF75;
CompFrame* HandleCompression(CompFrame* result)
{
	int compheader; // r2
	int size; // r2
	int dst = 0x2002000;//0x6000000;
	compheader = result->headervalue;
	//Copies a raw frame
	if (compheader == RAWHEADER)
	{
		size = result->size;
		/*   dword_40000D4 = &result->source;
		   dword_40000D8 = dst;
		   dword_40000DC = size | 0x80000000;*/
		*dma3_source = (unsigned int)&result->source;
		*dma3_destination = (unsigned int)dst;
		*dma3_control = DMA_ENABLE | DMA_16 | size;

	}
	else if (compheader == LZCOMPRESSEDHEADER)//Decompresses LZ from src lz to decomp dst
	{
		Lz77Uncomp(&result->source, dst);
		canDmaImage=1;
	}
	else if (compheader == DIFFHEADER)//Applies differences from a src frame and a target frame for minimal compression
	{
		UncompIPSRLE(&result->source, dst);
	}
	else if (compheader == QUADDIFFHEADER) //4 pointers, RLE IPS compressed. 
	{
		FrameCompareCompQuad(&result->source, dst);
	}
	else if (compheader == QUADDIFFHEADER2)//4 pointers, Decompress to specific areas of sceen, LT, RT, LB, RB
	{
		Exception(QUADDIFFHEADER2, "Not Supported");
	}
	else if (compheader == RLEHEADER)
	{
		Exception(RLEHEADER, "Not Supported");
	}
    else if(compheader == NINTYRLHEADER)
	{
        // FrameCompareCompQuadninty(&result->source, dst);
         
	}
    else if(compheader == NINTYRLHEADERINTR)
	{
         FrameCompare(&result->source, dst); 
		 canDmaImage=0;        
	}




	return result;
}
extern const int FrameCount;
extern const int FPS;

int main() {
	//set up screen
	(*(unsigned short*)0x4000000) = 0x403;
	Setup();
	//Start "naturally"
	FrameCounter = FrameCount;
	//fps
	int delay = Div(60, FPS);

	while (1)
	{
		if (vblankcounter % delay == 0) {
			CanDraw = 1;
		}
		if (FrameCounter >= FrameCount)
		{
			FrameCounter = 0;
			play_sound(muta, muta_size, 10512, 'A');
		}

		if (CanDraw) {

			VidFrame* frameInfo = &theframes[FrameCounter++];
			int v1 = frameInfo->size;
			HandleCompression(frameInfo->address);
			CanDraw = 0;
		}

		VBlankIntrWait();
	}
	return 0;
}