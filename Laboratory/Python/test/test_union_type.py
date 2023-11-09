from schemas import U, A, B, C, D, InnerM2

def test_write_read_A():
    a = A()
    a.b = 90

    u = U.fromA(a)

    assert u.isA() == True

    encoded = U.encode(u)
    
    assert encoded == [10, 0, 0, 0, 1, 6, 0, 0, 0, 1, 90, 0, 0, 0, 0]

    decoded = U.decode(encoded)

    assert decoded.discriminator == 1
    assert decoded.value.b == 90

def test_write_read_B():
    b = B(True)

    u = U.fromB(b)

    assert u.isB() == True

    encoded = U.encode(u)
    
    assert encoded == [1, 0, 0, 0, 2, 1]

    decoded = U.decode(encoded)

    assert decoded.discriminator == 2
    assert decoded.value.c == True

def test_write_read_C():
    c = C()

    u = U.fromC(c)

    assert u.isC() == True

    encoded = U.encode(u)
    
    assert encoded == [0, 0, 0, 0, 3]

    decoded = U.decode(encoded)

    assert decoded.discriminator == 3

def test_write_read_D():
    msg = InnerM2()
    msg.x = 90
    d = D(msg)

    u = U.fromD(d)

    assert u.isD() == True

    encoded = U.encode(u)
    
    assert encoded == [10, 0, 0, 0, 4, 6, 0, 0, 0, 1, 90, 0, 0, 0, 0]

    decoded = U.decode(encoded)

    assert decoded.discriminator == 4
    assert decoded.value.msg.x == 90
