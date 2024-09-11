PROJECT_DIR = $(shell realpath ../../src/vitalrouter-mruby)
CC = gcc
CFLAGS = -Wall -I$(PROJECT_DIR)/ext/mruby/include -I$(PROJECT_DIR)/src
LDFLAGS = -L. $(MRUBY_LIBPATH)

TARGET = ./vitalrouter-mruby-tests

SRC = main.c
OBJ = $(SRC:.c=.o)

$(TARGET): $(OBJ)
	$(CC) -o $(TARGET) $(OBJ) $(LDFLAGS)
	./$(TARGET)

%.o: %.c
	$(CC) $(CFLAGS) -c $< -o $@

clean:
	rm -f $(OBJ) $(TARGET)

.PHONY: clean
