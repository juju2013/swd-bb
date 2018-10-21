# swd-bb

( ARM's ) Serial Wire Debug, bit-bang version, on the cheap bluepill ( stm32f103 ) board

# What, Why

This implement a subset of ARM's swd on a stm32F103, AKA bluepill board.

It's specifically designed to do a mass_erase, ie., clear the protection bits
and ALL flash/ram contents for a nrf52832 MCU.

For the normal swd programming, better to use "normal" tools like openocd ou blackmagic probe.

## Motivation

Nordic's nrf52 SDK does provide some way to do this by using a SEGGER J-Link and some compiled binaries.
It's useless for "exotic" configurations, like a musl based system or no J-Link debugger.

Or, you can do it with a [blue pill](http://wiki.stm32duino.com/index.php?title=Blue_Pill), [Mecrisp stellaris Forth](http://mecrisp.sourceforge.net/),
some files from [Embello](https://github.com/jeelabs/embello) and this repo.

