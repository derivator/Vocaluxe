ARCH?=$(shell uname -m | sed -e s/i.86/x86/ -e s/x86_64/x64/)
ROOT_DIR:=$(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
all:
	$(MAKE) -C PitchTracker
	$(MAKE) -C Vocaluxe/Lib/Video/Acinerella
	xbuild /property:Platform=$(ARCH) /property:Configuration=ReleaseLinux /property:WorkaroundTargetDir="$(ROOT_DIR)/Output/"

clean:
	xbuild /target:Clean
	$(MAKE) -C PitchTracker clean
	$(MAKE) -C Vocaluxe/Lib/Video/Acinerella clean

INSTALLPATH?=/usr/share/vocaluxe
WRONG_ARCH=$(shell echo $(ARCH) | sed -e s/x86/x64/ -e s/x64/x86/)
install:
	mkdir -p $(DESTDIR)$(INSTALLPATH)
	cp -ru Output/. $(DESTDIR)$(INSTALLPATH)
	rm -rf $(DESTDIR)$(INSTALLPATH)/$(WRONG_ARCH)

ICONPATH?=/usr/share/applications
install_icon:
	mkdir -p $(DESTDIR)$(ICONPATH)
	cp Vocaluxe/Linux/Package/Vocaluxe.desktop $(DESTDIR)$(ICONPATH)/
	sed -i -e 's=INSTALLPATH=$(INSTALLPATH)=' $(DESTDIR)$(ICONPATH)/Vocaluxe.desktop

