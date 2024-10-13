class Object
  def is?(other) = self == other
  alias :is :is?
end

class NilClass
  def is?(other) = !other
  alias :is :is?
end

class FalseClass
  def is?(other) = !other
  alias :is :is?  
end

class TrueClass
  def is?(other) = !!other
  alias :is :is?  
end

class Symbol
  def is?(other) = self == other.to_sym
  alias :is :is?  
end

class String
  def is?(other) = self == other.to_s
  alias :is :is?  
end

class Numeric
  def secs = VitalRouter::TimeDuration.new(self.to_f)
  alias :sec :secs

  def millisecs = VitalRouter::TimeDuration.new(self * 0.001)
  alias :millisec :millisecs

  def frames = VitalRouter::FrameDuration.new(self.to_i)
  alias :frame :frames
end
